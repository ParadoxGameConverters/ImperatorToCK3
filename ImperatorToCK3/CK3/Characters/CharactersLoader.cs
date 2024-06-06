using commonItems;
using commonItems.Mods;
using Open.Collections.Synchronized;
using System.Linq;

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
		
		string[] irrelevantEffects = ["set_relation_rival", "set_relation_potential_rival", "set_relation_nemesis", 
			"set_relation_lover", "set_relation_soulmate",
			"set_relation_friend", "set_relation_potential_friend", "set_relation_best_friend",
			"set_relation_ward", "set_relation_mentor",];
		
		foreach (var character in loadedCharacters) {
			// Remove post-bookmark history except for births and deaths.
			foreach (var field in character.History.Fields) {
				if (field.Id == "birth" || field.Id == "death") {
					continue;
				}
				field.RemoveHistoryPastDate(bookmarkDate);
			}

			// Replace birth entries like "birth = "1081.1.1"" with "birth = yes".
			var birthField = character.History.Fields["birth"];
			birthField.RemoveAllEntries();
			birthField.AddEntryToHistory(character.BirthDate, "birth", value: true);
			
			// Replace complex death entries like "death = { death_reason = death_murder_known killer = 9051 }"
			// with "death = yes".
			Date? deathDate = character.DeathDate;
			if (deathDate is not null) {
				var deathField = character.History.Fields["death"];
				deathField.RemoveAllEntries();
				deathField.AddEntryToHistory(deathDate, "death", value: true);
			}
			
			// Remove effects that set relations. They don't matter a lot in our alternate timeline.
			character.History.Fields["effects"].RemoveAllEntries(
				entry => irrelevantEffects.Any(effect => entry.ToString()?.Contains(effect) ?? false));

			character.UpdateChildrenCacheOfParents();
		}
	}
}