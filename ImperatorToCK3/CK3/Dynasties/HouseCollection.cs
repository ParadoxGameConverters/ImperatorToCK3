using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Dynasties;

public class HouseCollection : ConcurrentIdObjectCollection<string, House> {
	public void LoadCK3Houses(ModFilesystem ck3ModFS) {
		Logger.Info("Loading dynasty houses from CK3...");
		
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, houseId) => {
			var house = new House(houseId, reader);
			AddOrReplace(house);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/dynasty_houses", ck3ModFS, "txt", recursive: true, parallel: true);
	}

	public void PurgeUnneededHouses(CharacterCollection ck3Characters, Date date) {
		Logger.Info("Purging unneeded dynasty houses...");

		HashSet<string> houseIdsToKeep = ck3Characters
			.Select(c => c.GetDynastyHouseId(date))
			.Where(id => id is not null)
			.Distinct()
			.Cast<string>()
			.ToHashSet();

		int removedCount = 0;
		foreach (var house in this.ToList()) {
			if (houseIdsToKeep.Contains(house.Id)) {
				continue;
			}

			Remove(house.Id);
			++removedCount;
		}
		Logger.Info($"Purged {removedCount} unneeded dynasty houses.");
	}
}