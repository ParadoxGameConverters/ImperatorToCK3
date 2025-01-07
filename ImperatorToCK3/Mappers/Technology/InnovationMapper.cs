using commonItems;
using commonItems.Localization;
using DotLiquid;
using ImperatorToCK3.Imperator.Inventions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Mappers.Technology;

internal sealed class InnovationMapper {
	private readonly List<InnovationLink> innovationLinks = [];
	private readonly List<InnovationBonus> innovationBonuses = [];

	public void LoadLinksAndBonuses(string configurablePath, OrderedDictionary<string, bool> ck3ModFlags) {
		// The file used the Liquid templating language, so convert it to text before parsing.
		var convertedModFlags = ck3ModFlags.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
		var context = Hash.FromDictionary(convertedModFlags);
		
		var liquidText = File.ReadAllText(configurablePath);
		var template = Template.Parse(liquidText);
		var result = template.Render(context);
		
		var parser = new Parser();
		parser.RegisterKeyword("link", reader => innovationLinks.Add(new InnovationLink(reader)));
		parser.RegisterKeyword("bonus", reader => innovationBonuses.Add(new InnovationBonus(reader)));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(new BufferedReader(result));
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
		var unmappedInventions = inventionsDB.InventionIds
			.Where(invention => !innovationLinks.Exists(link => link.Match(invention) is not null) && !innovationBonuses.Exists(bonus => bonus.GetProgress([invention]) is not null))
			.ToArray();

		var inventionsWithLoc = unmappedInventions
			.Select(inventionId => {
				if (irLocDB.GetLocBlockForKey(inventionId) is { } locBlock) {
					return $"{inventionId} ({locBlock[ConverterGlobals.PrimaryLanguage]})";
				}
				return inventionId;
			});

		Logger.Debug($"Unmapped I:R inventions: {string.Join(", ", inventionsWithLoc)}");
	}

	// TODO: ALSO LOG UNMAPPED CK3 MARTIAL AND CIVIC INNOVATIONS

	public void RemoveMappingsWithInvalidInnovations(ISet<string> innovationIds) {
		int removedCount = 0;

		removedCount += innovationLinks
			.RemoveAll(link => link.CK3InnovationId is null || !innovationIds.Contains(link.CK3InnovationId));
		removedCount += innovationBonuses
			.RemoveAll(bonus => bonus.CK3InnovationId is null || !innovationIds.Contains(bonus.CK3InnovationId));

		Logger.Debug($"Removed {removedCount} technology mappings with invalid CK3 innovations.");
	}
}
