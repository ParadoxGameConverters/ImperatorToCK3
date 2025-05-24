using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Dynasties;

internal sealed class HouseCollection : ConcurrentIdObjectCollection<string, House> {
	public void LoadCK3Houses(ModFilesystem ck3ModFS) {
		Logger.Info("Loading dynasty houses from CK3...");

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, houseId) => {
			var house = new House(houseId, reader);
			AddOrReplace(house);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/dynasty_houses", ck3ModFS, "txt", recursive: true);
	}

	public void PurgeUnneededHouses(CharacterCollection ck3Characters, Date date) {
		Logger.Info("Purging unneeded dynasty houses...");
		
		// Load IDs of houses that should always be kept.
		var houseIdsToPreserve = new HashSet<string>();
		var nonRemovableIdsParser = new Parser();
		nonRemovableIdsParser.RegisterRegex(CommonRegexes.String, (_, id) => {
			houseIdsToPreserve.Add(id);
		});
		nonRemovableIdsParser.IgnoreAndLogUnregisteredItems();
		nonRemovableIdsParser.ParseFile("configurables/dynasty_houses_to_preserve.txt");

		FrozenSet<string> houseIdsToKeep = ck3Characters
			.Select(c => c.GetDynastyHouseId(date))
			.Where(id => id is not null)
			.Distinct()
			.Cast<string>()
			.ToFrozenSet();

		int removedCount = 0;
		foreach (var house in this.ToArray()) {
			if (houseIdsToKeep.Contains(house.Id)) {
				continue;
			}
			if (houseIdsToPreserve.Contains(house.Id)) {
				continue;
			}

			Remove(house.Id);
			++removedCount;
		}
		Logger.Info($"Purged {removedCount} unneeded dynasty houses.");
	}
}