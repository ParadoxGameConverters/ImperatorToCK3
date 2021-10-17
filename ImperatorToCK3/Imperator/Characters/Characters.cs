﻿using commonItems;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.Imperator.Characters {
	public class Characters : Parser {
		public Characters() { }
		public Characters(BufferedReader reader, Genes.GenesDB? genesDB) {
			this.genesDB = genesDB;
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();

			Logger.Info("Linking Characters with Spouses...");
			LinkSpouses();
			Logger.Info("Linking Characters with Mothers and Fathers...");
			LinkMothersAndFathers();
		}
		public Dictionary<ulong, Character> StoredCharacters { get; } = new();
		public void LinkFamilies(Families.Families families) {
			var counter = 0;
			var idsWithoutDefinition = new SortedSet<ulong>();

			foreach (var character in StoredCharacters.Values) {
				if (character.LinkFamily(families, idsWithoutDefinition)) {
					++counter;
				}
			}

			if (idsWithoutDefinition.Count > 0) {
				Logger.Info($"Families without definition: {string.Join(", ", idsWithoutDefinition)}");
			}

			Logger.Info($"{counter} families linked to characters.");
		}
		private void LinkSpouses() {
			var spouseCounter = StoredCharacters.Values.Sum(character => character.LinkSpouses(StoredCharacters));
			Logger.Info($"{spouseCounter} spouses linked.");
		}

		private void LinkMothersAndFathers() {
			var motherCounter = 0;
			var fatherCounter = 0;
			foreach (var (characterID, character) in StoredCharacters) {
				var motherID = character.Mother.Key;
				if (motherID != 0) {
					if (StoredCharacters.TryGetValue(motherID, out var motherToLink)) {
						character.Mother = new(motherID, motherToLink);
						motherToLink.Children[characterID] = character;
						++motherCounter;
					} else {
						Logger.Warn($"Mother ID: {motherID} has no definition!");
					}
				}

				var fatherID = character.Father.Key;
				if (fatherID != 0) {
					if (StoredCharacters.TryGetValue(fatherID, out var fatherToLink)) {
						character.Father = new(fatherID, fatherToLink);
						fatherToLink.Children[characterID] = character;
						++fatherCounter;
					} else {
						Logger.Warn($"Father ID: {fatherID} has no definition!");
					}
				}
			}
			Logger.Info($"{motherCounter} mothers and {fatherCounter} fathers linked.");
		}

		public void LinkCountries(Countries.Countries countries) {
			var counter = 0;
			foreach (var (characterId, character) in StoredCharacters) {
				if (!character.Country.HasValue) {
					Logger.Warn($"Character {characterId} has no country!");
					continue;
				}
				var countryId = character.Country.Value.Key;
				if (countries.StoredCountries.TryGetValue(countryId, out var countryToLink)) {
					// link both ways
					character.Country = new(countryId, countryToLink);
					++counter;
				} else {
					Logger.Warn($"Country with ID {countryId} has no definition!");
				}
			}
			Logger.Info($"{counter} countries linked to characters.");
		}

		private void RegisterKeys() {
			RegisterRegex(CommonRegexes.Integer, (reader, charIdStr) => {
				var newCharacter = Character.Parse(reader, charIdStr, genesDB);
				StoredCharacters.Add(newCharacter.Id, newCharacter);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		private readonly Genes.GenesDB? genesDB;

		public static Characters ParseBloc(BufferedReader reader, Genes.GenesDB genesDB) {
			var blocParser = new Parser();
			var parsedCharacters = new Characters();
			blocParser.RegisterKeyword("character_database", reader => {
				parsedCharacters = new Characters(reader, genesDB);
			});
			blocParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			blocParser.ParseStream(reader);
			blocParser.ClearRegisteredRules();
			Logger.Debug("Ignored Character tokens: " + string.Join(", ", Character.IgnoredTokens));
			return parsedCharacters;
		}
	}
}
