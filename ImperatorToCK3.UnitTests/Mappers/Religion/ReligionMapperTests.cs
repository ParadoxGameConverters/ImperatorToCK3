using commonItems;
using ImperatorToCK3.Mappers.Religion;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Religion {
	public class ReligionMapperTests {
		[Fact]
		public void NonMatchGivesEmptyOptional() {
			var reader = new BufferedReader("link = { ck3 = ck3Religion imp = impReligion }");
			var impRegionMapper = new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper();
			var ck3RegionMapper = new ImperatorToCK3.Mappers.Region.CK3RegionMapper();
			var mapper = new ReligionMapper(reader, impRegionMapper, ck3RegionMapper);

			var ck3Religion = mapper.Match("nonMatchingReligion", 0, 0, new Configuration());
			Assert.Null(ck3Religion);
		}

		[Fact]
		public void Ck3ReligionCanBeFound() {
			var reader = new BufferedReader("link = { ck3 = ck3Religion imp = impReligion }");
			var impRegionMapper = new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper();
			var ck3RegionMapper = new ImperatorToCK3.Mappers.Region.CK3RegionMapper();
			var mapper = new ReligionMapper(reader, impRegionMapper, ck3RegionMapper);

			var ck3Religion = mapper.Match("impReligion", 45, 456, new Configuration());
			Assert.Equal("ck3Religion", ck3Religion);
		}

		[Fact]
		public void MultipleCK3ReligionsCanBeInARule() {
			var reader = new BufferedReader(
				"link = { ck3 = ck3Religion imp = impReligion imp = impReligion2 }"
			);
			var impRegionMapper = new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper();
			var ck3RegionMapper = new ImperatorToCK3.Mappers.Region.CK3RegionMapper();
			var mapper = new ReligionMapper(reader, impRegionMapper, ck3RegionMapper);

			var ck3Religion = mapper.Match("impReligion2", 45, 456, new Configuration());
			Assert.Equal("ck3Religion", ck3Religion);
		}

		[Fact]
		public void CorrectRuleMatches() {
			var reader = new BufferedReader(
				"link = { ck3 = ck3Religion imp = impReligion }" +
				"link = { ck3 = ck3Religion2 imp = impReligion2 }"
			);
			var impRegionMapper = new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper();
			var ck3RegionMapper = new ImperatorToCK3.Mappers.Region.CK3RegionMapper();
			var mapper = new ReligionMapper(reader, impRegionMapper, ck3RegionMapper);

			var ck3Religion = mapper.Match("impReligion2", 45, 456, new Configuration());
			Assert.Equal("ck3Religion2", ck3Religion);
		}
	}
}
