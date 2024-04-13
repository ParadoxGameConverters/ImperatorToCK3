using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Inventions;

public class InventionsDB {
	private readonly OrderedSet<string> inventions = [];
	
	public void LoadInventions(ModFilesystem irModFS) {
		var inventionsParser = new Parser();
		inventionsParser.RegisterKeyword("technology", ParserHelpers.IgnoreItem);
		inventionsParser.RegisterKeyword("color", ParserHelpers.IgnoreItem);
		inventionsParser.RegisterRegex(CommonRegexes.String, (reader, inventionId) => {
			inventions.Add(inventionId);
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
		foreach (var item in inventions.Select((inventionId, i) => new { i, inventionId })) {
			if (booleans[item.i]) {
				yield return item.inventionId;
			}
		}
	}
}