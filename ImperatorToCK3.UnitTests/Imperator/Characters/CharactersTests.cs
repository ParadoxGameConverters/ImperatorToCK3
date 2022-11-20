using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Characters {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class CharactersTests {
		private readonly ImperatorToCK3.Imperator.Genes.GenesDB genesDB = new();
		[Fact]
		public void CharactersDefaultToEmpty() {
			var characters = new ImperatorToCK3.Imperator.Characters.CharacterCollection {GenesDB = genesDB};
			Assert.Empty(characters);
		}

		[Fact]
		public void CharactersCanBeLoaded() {
			var reader = new BufferedReader(
				"= {\n" +
				"\t42={}\n" +
				"\t43={}\n" +
				"}"
			);
			var characters = new ImperatorToCK3.Imperator.Characters.CharacterCollection {GenesDB = genesDB};
			characters.LoadCharacters(reader);

			Assert.Collection(characters,
				character => Assert.Equal((ulong)42, character.Id),
				character => Assert.Equal((ulong)43, character.Id));
		}
		[Fact]
		public void SpousesAreLinked() {
			var reader = new BufferedReader(
				"= {\n" +
				"\t42={}\n" +
				"\t43={ spouse = { 42 } }\n" +
				"}"
			);
			var characters = new ImperatorToCK3.Imperator.Characters.CharacterCollection {GenesDB = genesDB};
			characters.LoadCharacters(reader);
			Assert.Equal((ulong)42, characters[43].Spouses[42].Id);
		}

		[Fact]
		public void MissingSpousesAreDropped() {
			var reader = new BufferedReader(
				"= {\n" +
				"\t43={ spouse = { 42 } }\n" + // character 42 is missing definition
				"}"
			);
			var characters = new ImperatorToCK3.Imperator.Characters.CharacterCollection {GenesDB = genesDB};
			characters.LoadCharacters(reader);
			Assert.Empty(characters[43].Spouses);
		}
	}
}
