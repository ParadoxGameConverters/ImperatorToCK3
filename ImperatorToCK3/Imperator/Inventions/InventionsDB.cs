using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Inventions;

public class InventionsDB {
	private readonly OrderedSet<string> inventionIds = [];

	public IReadOnlyCollection<string> InventionIds => inventionIds;
	
	public void LoadInventions(ModFilesystem irModFS) {
		var inventionsParser = new Parser();
		inventionsParser.RegisterKeyword("technology", ParserHelpers.IgnoreItem);
		inventionsParser.RegisterKeyword("color", ParserHelpers.IgnoreItem);
		inventionsParser.RegisterRegex(CommonRegexes.String, (reader, inventionId) => {
			inventionIds.Add(inventionId);
			ParserHelpers.IgnoreItem(reader);
		});
		inventionsParser.IgnoreAndLogUnregisteredItems();
		
		var inventionGroupsParser = new Parser();
		inventionGroupsParser.RegisterRegex(CommonRegexes.String, reader => inventionsParser.ParseStream(reader));
		inventionGroupsParser.IgnoreAndLogUnregisteredItems();
		
		Logger.Info("Loading Imperator inventions...");
		inventionGroupsParser.ParseGameFolder("common/inventions", irModFS, "txt", recursive: true);
	}

	public IEnumerable<string> GetActiveInventionIds(IList<bool> booleans) {
		// Enumerate over the inventions and return the ones that are active (bool is true).
		foreach (var item in inventionIds.Select((inventionId, i) => new { i, inventionId })) {
			if (item.i < booleans.Count && booleans[item.i]) {
				yield return item.inventionId;
			}
		}
	}
}