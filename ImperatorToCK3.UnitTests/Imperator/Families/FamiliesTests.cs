using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Families {
	public class FamiliesTests {
		[Fact]
		public void FamiliesDefaultToEmpty() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
				"}"
			);
			var families = new ImperatorToCK3.Imperator.Families.Families();
			families.LoadFamilies(reader);

			Assert.Empty(families.StoredFamilies);
		}

		[Fact]
		public void FamiliesCanBeLoaded() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
				"42={}\n" +
				"43={}\n" +
				"}"
			);
			var families = new ImperatorToCK3.Imperator.Families.Families();
			families.LoadFamilies(reader);

			Assert.Collection(families.StoredFamilies,
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

		[Fact]
		public void LiteralNoneFamiliesAreNotLoaded() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
				"42=none\n" +
				"43={}\n" +
				"44=none\n" +
				"}"
			);
			var families = new ImperatorToCK3.Imperator.Families.Families();
			families.LoadFamilies(reader);

			Assert.Collection(families.StoredFamilies,
				item => {
					Assert.Equal((ulong)43, item.Key);
					Assert.Equal((ulong)43, item.Value.ID);
				}
			);
		}
	}
}
