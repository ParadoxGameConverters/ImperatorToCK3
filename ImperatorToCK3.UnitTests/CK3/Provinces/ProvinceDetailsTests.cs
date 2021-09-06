using ImperatorToCK3.CK3.Provinces;
using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Provinces {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
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

		[Fact]
		public void DetailsCanBeCopyConstructed() {
			var reader = new BufferedReader(
				"= {" +
				"\treligion = catholic\n" +
				"\tculture = roman\n" +
				"\tbuildings = { orchard tavern }" +
				"\t850.1.1 = { religion=orthodox holding=castle_holding }" +
				"}"
			);
			var details1 = new ProvinceDetails(reader);
			var details2 = new ProvinceDetails(details1);

			Assert.Equal("castle_holding", details2.Holding);
			Assert.Equal("orthodox", details2.Religion);
			Assert.Equal("roman", details2.Culture);
			Assert.Collection(details2.Buildings,
				item1 => Assert.Equal("orchard", item1),
				item2 => Assert.Equal("tavern", item2)
			);
		}
	}
}
