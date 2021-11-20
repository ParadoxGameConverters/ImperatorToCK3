using commonItems;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ImperatorToCK3.Imperator.Characters {
	public class Characters {
		public Characters() { }
		public Characters(BufferedReader reader, Genes.GenesDB? genesDB) {
			this.genesDB = genesDB;
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);

			Logger.Info("Linking Characters with Spouses...");
			LinkSpouses();
			Logger.Info("Linking Characters with Mothers and Fathers...");
			LinkMothersAndFathers();
		}
		private readonly Dictionary<ulong, Character> charactersDict = new();
		public Dictionary<ulong, Character>.ValueCollection StoredCharacters => charactersDict.Values;

		public void Add(Character character) {
			charactersDict.Add(character.Id, character);
		}
		public bool TryGetCharacter(ulong characterId, [NotNullWhen(returnValue: true)] out Character? character) {
			return charactersDict.TryGetValue(characterId, out character);
		}
		public Character this[ulong id] => charactersDict[id];

		public void LinkFamilies(Families.Families families) {
			var idsWithoutDefinition = new SortedSet<ulong>();
			var counter = charactersDict.Values.Count(character => character.LinkFamily(families, idsWithoutDefinition));
			if (idsWithoutDefinition.Count > 0) {
				Logger.Info($"Families without definition: {string.Join(", ", idsWithoutDefinition)}");
			}

			Logger.Info($"{counter} families linked to characters.");
		}
		private void LinkSpouses() {
			var spouseCounter = charactersDict.Values.Sum(character => character.LinkSpouses(charactersDict));
			Logger.Info($"{spouseCounter} spouses linked.");
		}

		private void LinkMothersAndFathers() {
			var motherCounter = 0;
			var fatherCounter = 0;
			foreach (var character in charactersDict.Values) {
				if (character.LinkMother(this)) {
					++motherCounter;
				}
				if (character.LinkFather(this)) {
					++fatherCounter;
				}
			}
			Logger.Info($"{motherCounter} mothers and {fatherCounter} fathers linked.");
		}

		public void LinkCountries(Countries.Countries countries) {
			var counter = charactersDict.Values.Count(character => character.LinkCountry(countries));
			Logger.Info($"{counter} countries linked to characters.");

			counter = charactersDict.Values.Count(character => character.LinkPrisonerHome(countries));
			Logger.Info($"{counter} prisoner homes linked to characters.");
		}

		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, charIdStr) => {
				var newCharacter = Character.Parse(reader, charIdStr, genesDB);
				Add(newCharacter);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
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
