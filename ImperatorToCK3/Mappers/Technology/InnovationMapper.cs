using commonItems;
using commonItems.Localization;
using ImperatorToCK3.Imperator.Inventions;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Technology;

public class InnovationMapper {
	private readonly List<InnovationLink> innovationLinks = [];
	private readonly List<InnovationBonus> innovationBonuses = [];
	
	public void LoadLinksAndBonuses(string configurablePath) {
		var parser = new Parser();
		parser.RegisterKeyword("link", reader => innovationLinks.Add(new InnovationLink(reader)));
		parser.RegisterKeyword("bonus", reader => innovationBonuses.Add(new InnovationBonus(reader)));
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(configurablePath);
	}

	public IList<string> GetInnovations(IEnumerable<string> irInventions) {
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

	public IDictionary<string, ushort> GetInnovationProgresses(ICollection<string> irInventions) {
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
			.Where(invention => !innovationLinks.Exists(link => link.Match(invention) is not null))
			.Where(invention => !innovationBonuses.Exists(bonus => bonus.GetProgress([invention]) is not null))
			.ToList();
		
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
}
