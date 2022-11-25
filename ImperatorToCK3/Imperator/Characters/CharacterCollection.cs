using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Families;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Characters {
	public class CharacterCollection : IdObjectCollection<ulong, Character> {
		public CharacterCollection() { }

		public void LoadCharactersFromBloc(BufferedReader reader) {
			var blocParser = new Parser();
			blocParser.RegisterKeyword("character_database", LoadCharacters);
			blocParser.IgnoreAndLogUnregisteredItems();

			blocParser.ParseStream(reader);
			Logger.Debug($"Ignored Character tokens: {Character.IgnoredTokens}");
		}
		public void LoadCharacters(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);

			Logger.Info("Linking Characters with Spouses...");
			LinkSpouses();
			Logger.Info("Linking Characters with Mothers and Fathers...");
			LinkMothersAndFathers();
		}

		public void LinkFamilies(FamilyCollection families) {
			var idsWithoutDefinition = new SortedSet<ulong>();
			var counter = this.Count(character => character.LinkFamily(families, idsWithoutDefinition));
			if (idsWithoutDefinition.Count > 0) {
				Logger.Info($"Families without definition: {string.Join(", ", idsWithoutDefinition)}");
			}

			Logger.Info($"{counter} families linked to characters.");
		}
		private void LinkSpouses() {
			var spouseCounter = this.Sum(character => character.LinkSpouses(this));
			Logger.Info($"{spouseCounter} spouses linked.");
		}

		private void LinkMothersAndFathers() {
			var motherCounter = 0;
			var fatherCounter = 0;
			foreach (var character in this) {
				if (character.LinkMother(this)) {
					++motherCounter;
				}
				if (character.LinkFather(this)) {
					++fatherCounter;
				}
			}
			Logger.Info($"{motherCounter} mothers and {fatherCounter} fathers linked.");
		}

		public void LinkCountries(CountryCollection countries) {
			var counter = this.Count(character => character.LinkCountry(countries));
			Logger.Info($"{counter} countries linked to characters.");

			counter = this.Count(character => character.LinkHomeCountry(countries));
			Logger.Info($"{counter} home countries linked to characters.");

			counter = this.Count(character => character.LinkPrisonerHome(countries));
			Logger.Info($"{counter} prisoner homes linked to characters.");
		}

		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, charIdStr) => {
				var newCharacter = Character.Parse(reader, charIdStr, GenesDB);
				Add(newCharacter);
			});
			parser.IgnoreAndLogUnregisteredItems();
		}
		
		public Genes.GenesDB? GenesDB { get; set; }
	}
}
