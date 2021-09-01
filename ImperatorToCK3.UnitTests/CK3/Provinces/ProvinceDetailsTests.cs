using ImperatorToCK3.CK3.Provinces;
using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Provinces {
	public class ProvinceDetailsTests {
		[Fact] public void FieldsDefaultToCorrectValues() {
			var details = new ProvinceDetails();
			Assert.Equal(string.Empty, details.Culture);
			Assert.Equal(string.Empty, details.Religion);
			Assert.Equal("none", details.Holding);
			Assert.Empty(details.Buildings);
		}

		[Fact]
		public void DetailsCanBeLoadedFromStream() {
			var reader = new BufferedReader(
				"= { religion = orthodox\n random_param = random_stuff\n culture = roman\n}"
			);
			var details = new ProvinceDetails(reader);

			Assert.Equal("roman", details.Culture);
			Assert.Equal("orthodox", details.Religion);
		}

		[Fact]
		public void DetailsAreLoadedFromDatedBlocks() {
			var reader = new BufferedReader(
				"= {" +
				"religion = catholic\n" +
				"random_param = random_stuff\n" +
				"culture = roman\n" +
				"850.1.1 = { religion=orthodox holding=castle_holding }" +
				"}"
			);
			var details = new ProvinceDetails(reader);

			Assert.Equal("castle_holding", details.Holding);
			Assert.Equal("orthodox", details.Religion);
		}
	}
}
