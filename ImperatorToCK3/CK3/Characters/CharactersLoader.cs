using commonItems;
using commonItems.Mods;
using Open.Collections.Synchronized;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Characters;

internal sealed partial class CharacterCollection {
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
		parser.ParseGameFolder("history/characters", ck3ModFS, "txt", recursive: true);
		
		// Make all animation_test_ characters die on 2.1.1.
		foreach (var character in loadedCharacters) {
			if (!character.Id.StartsWith("animation_test_")) {
				continue;
			}
			
			var deathField = character.History.Fields["death"];
			deathField.RemoveAllEntries();
			deathField.AddEntryToHistory(new Date(2, 1, 1), "death", value: true);
		}

		string[] irrelevantEffects = ["set_relation_rival", "set_relation_potential_rival", "set_relation_nemesis",
			"set_relation_lover", "set_relation_soulmate",
			"set_relation_friend", "set_relation_potential_friend", "set_relation_best_friend",
			"set_relation_ward", "set_relation_mentor",
			"add_opinion", "make_concubine",
		];
		string[] fieldsToClear = [
			"friends", "best_friends", "lovers", "rivals", "nemesis",
			"primary_title", "dna", "spawn_army", "add_character_modifier", "languages",
			"claims",
		];

		var femaleCharacterIds = loadedCharacters.Where(c => c.Female).Select(c => c.Id).ToHashSet();
		var maleCharacterIds = loadedCharacters.Select(c => c.Id).Except(femaleCharacterIds).ToHashSet();
		
		foreach (var character in loadedCharacters) {
			// Clear some fields we don't need.
			foreach (var fieldName in fieldsToClear) {
				character.History.Fields[fieldName].RemoveAllEntries();
			}

			// Remove post-bookmark history except for births and deaths.
			foreach (var field in character.History.Fields) {
				if (field.Id == "birth" || field.Id == "death") {
					continue;
				}
				field.RemoveHistoryPastDate(bookmarkDate);
			}

			// Replace birth entries like "birth = "1081.1.1"" with "birth = yes".
			var birthDate = character.BirthDate;
			var birthField = character.History.Fields["birth"];
			birthField.RemoveAllEntries();
			birthField.AddEntryToHistory(birthDate, "birth", value: true);

			// Replace complex death entries like "death = { death_reason = death_murder_known killer = 9051 }"
			// with "death = yes".
			Date? deathDate = character.DeathDate;
			if (deathDate is not null) {
				var deathField = character.History.Fields["death"];
				deathField.RemoveAllEntries();
				deathField.AddEntryToHistory(deathDate, "death", value: true);
			}

			RemoveInvalidMotherAndFatherEntries(character, femaleCharacterIds, maleCharacterIds);

			// Remove dated name changes like 64.10.13 = { name = "Linus" }
			var nameField = character.History.Fields["name"];
			nameField.RemoveHistoryPastDate(birthDate);

			// Remove effects that set relations. They don't matter a lot in our alternate timeline.
			character.History.Fields["effects"].RemoveAllEntries(
				entry => irrelevantEffects.Any(effect => entry.ToString()?.Contains(effect) ?? false));
			
			character.InitSpousesCache();
			character.InitConcubinesCache();
			character.UpdateChildrenCacheOfParents();
		}
		
		Logger.Info("Loaded CK3 characters.");
	}

	private static void RemoveInvalidMotherAndFatherEntries(Character character, HashSet<string> femaleCharacterIds, HashSet<string> maleCharacterIds) {
		// Remove wrong sex mother and father references (male mothers, female fathers).
		var motherField = character.History.Fields["mother"];
		motherField.RemoveAllEntries(value => {
			string? motherId = value.ToString()?.RemQuotes();
			if (motherId is null || !femaleCharacterIds.Contains(motherId)) {
				Logger.Debug($"Removing invalid mother {motherId} from character {character.Id}");
				return true;
			}

			return false;
		});
		
		var fatherField = character.History.Fields["father"];
		fatherField.RemoveAllEntries(value => {
			string? fatherId = value.ToString()?.RemQuotes();
			if (fatherId is null || !maleCharacterIds.Contains(fatherId)) {
				Logger.Debug($"Removing invalid father {fatherId} from character {character.Id}");
				return true;
			}

			return false;
		});
	}
}