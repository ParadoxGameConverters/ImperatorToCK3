using System.Collections.Generic;
using System.Text;
using commonItems;

namespace ImperatorToCK3.Imperator.Characters {
	public class Characters : Parser {
		public Characters() { }
		public Characters(BufferedReader reader, Genes.GenesDB? genesDB) {
			this.genesDB = genesDB;
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		public Dictionary<ulong, Character?> StoredCharacters = new();
		public void LinkFamilies(Families.Families families) {
			var counter = 0;
			var idsWithoutDefinition = new SortedSet<ulong>();

			foreach (var (characterID, character) in StoredCharacters) {
				if (character is null) {
					Logger.Warn($"Cannot link family to null character {characterID}.");
					continue;
				}
				var familyID = character.Family.Key;
				if (families.StoredFamilies.TryGetValue(familyID, out var familyToLink)) {
					if (familyToLink is null) {
						Logger.Warn($"Cannot link null family {familyID} to character {characterID}.");
						continue;
					}
					character.Family = new(familyID, familyToLink);
					familyToLink.LinkMember(character);
					++counter;
				} else {
					idsWithoutDefinition.Add(familyID);
				}
			}

			if (idsWithoutDefinition.Count > 0) {
				var logBuilder = new StringBuilder();
				logBuilder.Append("Families without definition:");
				foreach (var id in idsWithoutDefinition) {
					logBuilder.Append(' ');
					logBuilder.Append(id);
					logBuilder.Append(',');
				}
				Logger.Info(logBuilder.ToString()[0..^1]); // remove last comma
			}

			Logger.Info($"{counter} families linked to characters.");
		}
		public void LinkSpouses() {
			var spouseCounter = 0;
			foreach (var (characterID, character) in StoredCharacters) {
				if (character is null) {
					Logger.Warn($"Cannot link spouse to null character {characterID}.");
					continue;
				}
				if (character.Spouses.Count == 0) {
					continue;
				}
				var newSpouses = new Dictionary<ulong, Character?>();
				foreach (var spouseID in character.Spouses.Keys) {
					if (StoredCharacters.TryGetValue(spouseID, out var spouseToLink)) {
						if (spouseToLink is null) {
							Logger.Warn($"Cannot link null spouse {spouseID} to character {characterID}.");
							continue;
						}
						newSpouses.Add(spouseToLink.ID, spouseToLink);
						++spouseCounter;
					} else {
						Logger.Warn($"Spouse ID: {spouseID} has no definition!");
					}
				}
				character.Spouses = newSpouses;
			}
			Logger.Info($"{spouseCounter} spouses linked.");
		}
		public void LinkMothersAndFathers() {
			var motherCounter = 0;
			var fatherCounter = 0;
			foreach (var (characterID, character) in StoredCharacters) {
				if (character is null) {
					Logger.Warn($"Cannot link parents to null character {characterID}.");
					continue;
				}
				var motherID = character.Mother.Key;
				if (motherID != 0) {
					if (StoredCharacters.TryGetValue(motherID, out var motherToLink)) {
						if (motherToLink is null) {
							Logger.Warn($"Cannot link null mother {motherID} to character {characterID}.");
							continue;
						}
						character.Mother = new(motherID, motherToLink);
						motherToLink.Children.Add(characterID, character);
						++motherCounter;
					} else {
						Logger.Warn($"Mother ID: {motherID} has no definition!");
					}
				}

				var fatherID = character.Father.Key;
				if (fatherID != 0) {
					if (StoredCharacters.TryGetValue(fatherID, out var fatherToLink)) {
						if (fatherToLink is null) {
							Logger.Warn($"Cannot link null father {fatherID} to character {characterID}.");
							continue;
						}
						character.Father = new(fatherID, fatherToLink);
						fatherToLink.Children.Add(characterID, character);
						++fatherCounter;
					} else {
						Logger.Warn($"Father ID: {fatherID} has no definition!");
					}
				}
			}
			Logger.Info($"{motherCounter} mothers and {fatherCounter} fathers linked.");
		}
		private void RegisterKeys() {
			RegisterRegex(CommonRegexes.Integer, (reader, charIdStr) => {
				var newCharacter = Character.Parse(reader, charIdStr, genesDB);
				StoredCharacters.Add(newCharacter.ID, newCharacter);
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
			return parsedCharacters;
		}
	}
}
