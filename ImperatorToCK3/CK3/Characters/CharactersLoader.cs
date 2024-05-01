using commonItems;
using commonItems.Mods;
using Open.Collections.Synchronized;

namespace ImperatorToCK3.CK3.Characters;

public partial class CharacterCollection {
	public void LoadCK3Characters(ModFilesystem ck3ModFS, Date bookmarkDate) {
		Logger.Info("Loading characters from CK3...");

		var loadedCharacters = new ConcurrentList<Character>();
			
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, characterId) => {
			var character = new Character(characterId, reader, this);
			
			// Check if character has a birth date:
			if (character.History.Fields["birth"].DateToEntriesDict.Count == 0) {
				Logger.Debug($"Ignoring character {characterId} with no valid birth date.");
				return;
			}
			
			AddOrReplace(character);
			loadedCharacters.Add(character);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("history/characters", ck3ModFS, "txt", recursive: true, parallel: true);
		
		foreach (var character in loadedCharacters) {
			// Remove post-bookmark history except for births and deaths.
			foreach (var field in character.History.Fields) {
				if (field.Id == "birth" || field.Id == "death") {
					continue;
				}
				field.RemoveHistoryPastDate(bookmarkDate);
			}
			
			// Replace complex death entries like "death = { death_reason = death_murder_known killer = 9051 }"
			// with "death = yes".
			Date? deathDate = character.DeathDate;
			if (deathDate is not null) {
				var deathField = character.History.Fields["death"];
				deathField.RemoveAllEntries();
				deathField.AddEntryToHistory(deathDate, "death", value: true);
			}

			character.UpdateChildrenCacheOfParents();
		}
	}
}