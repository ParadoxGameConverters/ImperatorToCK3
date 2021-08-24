using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Characters {
	public class CharactersTests {
		private readonly ImperatorToCK3.Imperator.Genes.GenesDB genesDB = new();
		[Fact]
		public void CharactersDefaultToEmpty() {
			var reader = new BufferedReader("={}");
			var characters = new ImperatorToCK3.Imperator.Characters.Characters(reader, genesDB);
			Assert.Empty(characters.StoredCharacters);
		}

		[Fact]
		public void CharactersCanBeLoaded() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
				"\t42={}\n" +
				"\t43={}\n" +
				"}"
			);
			var characters = new ImperatorToCK3.Imperator.Characters.Characters(reader, genesDB);

			Assert.Collection(characters.StoredCharacters,
				item => {
					Assert.Equal((ulong)42, item.Key);
					Assert.Equal((ulong)42, item.Value.ID);
				},
				item => {
					Assert.Equal((ulong)43, item.Key);
					Assert.Equal((ulong)43, item.Value.ID);
				}
			);
		}
	}
}
