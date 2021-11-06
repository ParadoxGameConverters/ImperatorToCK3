using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Region;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Culture {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class CultureMappingTests {
		private const string islandRegionPath = "TestFiles/regions/island_regions.txt";
		[Fact]
		public void MatchOnRegion() {
			var ck3RegionMapper = new CK3RegionMapper();
			var landedTitles = new LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"k_ghef = { d_hujhu = { c_defff = { b_newbarony2 = { province = 4 } } } }"
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CultureMappingTests/MatchOnRegion.txt";
			ck3RegionMapper.LoadRegions(landedTitles, regionPath, islandRegionPath);

			var reader = new BufferedReader(
				"ck3 = dutch imp = german ck3Region = test_region1"
			);
			var theMapping = CultureMappingRule.Parse(reader);
			theMapping.CK3RegionMapper = ck3RegionMapper;
			theMapping.ImperatorRegionMapper = new ImperatorRegionMapper();

			Assert.Equal("dutch", theMapping.Match("german", "", 4, 0, ""));
		}

		[Fact]
		public void MatchOnRegionFailsForWrongRegion() {
			var ck3RegionMapper = new CK3RegionMapper();
			var landedTitles = new LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } } } } \n" +
				"k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } } } } \n"
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CultureMappingTests/MatchOnRegionFailsForWrongRegion";
			ck3RegionMapper.LoadRegions(landedTitles, regionPath, islandRegionPath);

			var reader = new BufferedReader(
				"ck3 = dutch imp = german ck3Region = test_region2"
			);
			var theMapping = CultureMappingRule.Parse(reader);
			theMapping.CK3RegionMapper = ck3RegionMapper;
			theMapping.ImperatorRegionMapper = new ImperatorRegionMapper();

			Assert.Null(theMapping.Match("german", "", 79, 0, ""));
		}

		[Fact]
		public void MatchOnRegionFailsForNoRegion() {
			var ck3RegionMapper = new CK3RegionMapper();
			var landedTitles = new LandedTitles();
			var landedTitlesReader = new BufferedReader(string.Empty);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CultureMappingTests/empty.txt";
			ck3RegionMapper.LoadRegions(landedTitles, regionPath, islandRegionPath);

			var reader = new BufferedReader(
				"ck3 = dutch imp = german ck3Region = test_region3"
			);
			var theMapping = CultureMappingRule.Parse(reader);
			theMapping.CK3RegionMapper = ck3RegionMapper;
			theMapping.ImperatorRegionMapper = new ImperatorRegionMapper();

			Assert.Null(theMapping.Match("german", "", 17, 0, ""));
		}

		[Fact]
		public void MatchOnRegionFailsForNoProvince() {
			var ck3RegionMapper = new CK3RegionMapper();
			var landedTitles = new LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n"
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CultureMappingTests/empty.txt";
			ck3RegionMapper.LoadRegions(landedTitles, regionPath, islandRegionPath);

			var reader = new BufferedReader(
				"ck3 = dutch imp = german ck3Region = d_hujhu"
			);
			var theMapping = CultureMappingRule.Parse(reader);
			theMapping.CK3RegionMapper = ck3RegionMapper;
			theMapping.ImperatorRegionMapper = new ImperatorRegionMapper();

			Assert.Null(theMapping.Match("german", "", 0, 0, ""));
		}
	}
}
