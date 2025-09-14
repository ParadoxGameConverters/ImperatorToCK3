using commonItems;
using commonItems.Localization;
using ImperatorToCK3.CK3;
using ImperatorToCK3.Imperator.Inventions;
using System.Collections.Generic;
using ZLinq;

namespace ImperatorToCK3.Mappers.Technology;

internal sealed class InnovationMapper {
	private readonly List<InnovationLink> innovationLinks = [];
	private readonly List<InnovationBonus> innovationBonuses = [];

	public void LoadLinksAndBonuses(string configurablePath, OrderedDictionary<string, bool> ck3ModFlags) {
		var parser = new Parser();
		parser.RegisterKeyword("link", reader => innovationLinks.Add(new InnovationLink(reader)));
		parser.RegisterKeyword("bonus", reader => innovationBonuses.Add(new InnovationBonus(reader)));
		parser.IgnoreAndLogUnregisteredItems();
		
		// The file uses the Liquid templating language.
		parser.ParseLiquidFile(configurablePath, ck3ModFlags);
	}

	public List<string> GetInnovations(IEnumerable<string> irInventions) {
		var ck3Innovations = new List<string>();
		foreach (var irInvention in irInventions) {
			foreach (var link in innovationLinks) {
				var match = link.Match(irInvention);
				if (match is not null) {
					ck3Innovations.Add(match);
				}
			}
		}
		return ck3Innovations;
	}

	public Dictionary<string, ushort> GetInnovationProgresses(ICollection<string> irInventions) {
		Dictionary<string, ushort> progressesToReturn = [];
		foreach (var bonus in innovationBonuses) {
			var innovationProgress = bonus.GetProgress(irInventions);
			if (!innovationProgress.HasValue) {
				continue;
			}

			if (progressesToReturn.TryGetValue(innovationProgress.Value.Key, out ushort currentValue)) {
				// Only the highest progress should be kept.
				if (currentValue < innovationProgress.Value.Value) {
					progressesToReturn[innovationProgress.Value.Key] = innovationProgress.Value.Value;
				}
			} else {
				progressesToReturn[innovationProgress.Value.Key] = innovationProgress.Value.Value;
			}
		}
		return progressesToReturn;
	}

	public void LogUnmappedInventions(InventionsDB inventionsDB, LocDB irLocDB) {
		// Log Imperator inventions for which neither link nor bonus for CK3 innovations exists.
		var unmappedInventions = inventionsDB.InventionIds.AsValueEnumerable()
			.Where(invention => !innovationLinks.Exists(link => link.Match(invention) is not null) && !innovationBonuses.Exists(bonus => bonus.GetProgress([invention]) is not null))
			.ToArray();

		var inventionsWithLoc = unmappedInventions.AsValueEnumerable()
			.Select(inventionId => {
				if (irLocDB.GetLocBlockForKey(inventionId) is { } locBlock) {
					return $"{inventionId} ({locBlock[ConverterGlobals.PrimaryLanguage]})";
				}
				return inventionId;
			});

		Logger.Debug($"Unmapped I:R inventions: {inventionsWithLoc.JoinToString(", ")}");
	}

	// TODO: ALSO LOG UNMAPPED CK3 MARTIAL AND CIVIC INNOVATIONS

	public void RemoveMappingsWithInvalidInnovations(HashSet<string> innovationIds) {
		int removedCount = 0;

		removedCount += innovationLinks
			.RemoveAll(link => link.CK3InnovationId is null || !innovationIds.Contains(link.CK3InnovationId));
		removedCount += innovationBonuses
			.RemoveAll(bonus => bonus.CK3InnovationId is null || !innovationIds.Contains(bonus.CK3InnovationId));

		Logger.Debug($"Removed {removedCount} technology mappings with invalid CK3 innovations.");
	}
}
