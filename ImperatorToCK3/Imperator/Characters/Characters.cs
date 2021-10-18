using commonItems;
using System.Collections.Generic;
using System.Linq;

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
			var idsWithoutDefinition = new SortedSet<ulong>();
			var counter = StoredCharacters.Values.Count(character => character.LinkFamily(families, idsWithoutDefinition));
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
			foreach (var (characterId, character) in StoredCharacters) {
				var motherId = character.Mother.Key;
				if (motherId != 0) {
					if (StoredCharacters.TryGetValue(motherId, out var motherToLink)) {
						character.Mother = new(motherId, motherToLink);
						motherToLink.Children[characterId] = character;
						++motherCounter;
					} else {
						Logger.Warn($"Mother ID: {motherId} has no definition!");
					}
				}

				var fatherId = character.Father.Key;
				if (fatherId != 0) {
					if (StoredCharacters.TryGetValue(fatherId, out var fatherToLink)) {
						character.Father = new(fatherId, fatherToLink);
						fatherToLink.Children[characterId] = character;
						++fatherCounter;
					} else {
						Logger.Warn($"Father ID: {fatherId} has no definition!");
					}
				}
			}
			Logger.Info($"{motherCounter} mothers and {fatherCounter} fathers linked.");
		}

		public void LinkCountries(Countries.Countries countries) {
			var counter = StoredCharacters.Values.Count(character => character.LinkCountry(countries));
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
