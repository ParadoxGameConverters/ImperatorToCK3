using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Families {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class FamiliesTests {
		[Fact]
		public void FamiliesDefaultToEmpty() {
			var reader = new BufferedReader(
				"= {}"
			);
			var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
			families.LoadFamilies(reader);

			Assert.Empty(families);
		}

		[Fact]
		public void FamiliesCanBeLoaded() {
			var reader = new BufferedReader(
				"= {\n" +
				"42={}\n" +
				"43={}\n" +
				"}"
			);
			var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
			families.LoadFamilies(reader);

			Assert.Collection(families,
				item => Assert.Equal((ulong)42, item.Id),
				item => Assert.Equal((ulong)43, item.Id));
		}

		[Fact]
		public void LiteralNoneFamiliesAreNotLoaded() {
			var reader = new BufferedReader(
				"={\n" +
				"42=none\n" +
				"43={}\n" +
				"44=none\n" +
				"}"
			);
			var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
			families.LoadFamilies(reader);

			Assert.Collection(families,
				item => Assert.Equal((ulong)43, item.Id)
			);
		}
	}
}
