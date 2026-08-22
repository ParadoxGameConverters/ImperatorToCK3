using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using System;
using System.Collections.Generic;
using ZLinq;

namespace ImperatorToCK3.CK3.Dynasties;

internal sealed class HouseCollection : ConcurrentIdObjectCollection<string, House> {
	public void LoadCK3Houses(ModFilesystem ck3ModFS) {
		Logger.Info("Loading dynasty houses from CK3...");

		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterRegex(CommonRegexes.String, (reader, houseId) => {
			var house = new House(houseId, reader);
			AddOrReplace(house);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/dynasty_houses", ck3ModFS, "txt", recursive: true);
		
		// Also load IDs of houses that should always be kept.
		var nonRemovableIdsParser = new Parser(implicitVariableHandling: false);
		nonRemovableIdsParser.RegisterRegex(CommonRegexes.String, (_, id) => {
			houseIdsConfiguredToBeKept.Add(id);
		});
		nonRemovableIdsParser.IgnoreAndLogUnregisteredItems();
		nonRemovableIdsParser.ParseFile("configurables/dynasty_houses_to_preserve.txt");
	}

	internal void PurgeUnneededHouses(CharacterCollection ck3Characters, Date date) {
		Logger.Info("Purging unneeded dynasty houses...");

		HashSet<string> houseIdsToKeep = new(StringComparer.Ordinal);
		foreach (var character in ck3Characters) {
			if (character.GetDynastyHouseId(date) is string houseId) {
				houseIdsToKeep.Add(houseId);
			}
		}

		int removedCount = 0;
		foreach (var house in this.AsValueEnumerable().ToArray()) {
			if (houseIdsToKeep.Contains(house.Id)) {
				continue;
			}
			if (houseIdsConfiguredToBeKept.Contains(house.Id)) {
				continue;
			}

			Remove(house.Id);
			++removedCount;
		}
		Logger.Info($"Purged {removedCount} unneeded dynasty houses.");
	}

	internal void RemoveUnlessConfiguredToPreserve(string houseId) {
		if (!houseIdsConfiguredToBeKept.Contains(houseId)) {
			Remove(houseId);
		}
	}

	readonly HashSet<string> houseIdsConfiguredToBeKept = [];
}