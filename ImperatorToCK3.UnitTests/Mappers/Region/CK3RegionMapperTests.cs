using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Region {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class CK3RegionMapperTests {
		private const string islandRegionPath = "TestFiles/regions/island_regions.txt";
		[Fact]
		public void RegionMapperCanBeEnabled() {
			// We start humble, it's a machine.
			var mapper = new CK3RegionMapper();
			var landedTitles = new Title.LandedTitles();
			const string regionPath = "TestFiles/regions/CK3RegionMapperTests/empty.txt";

			mapper.LoadRegions(landedTitles, regionPath, islandRegionPath);
			Assert.False(mapper.ProvinceIsInRegion(1, "test"));
			Assert.False(mapper.RegionNameIsValid("test"));
			Assert.Null(mapper.GetParentCountyName(1));
			Assert.Null(mapper.GetParentDuchyName(1));
			Assert.Null(mapper.GetParentRegionName(1));
		}
		[Fact]
		public void LoadingBrokenRegionWillThrowException() {
			var mapper = new CK3RegionMapper();
			var landedTitles = new Title.LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"k_anglia = { d_aquitane = { c_mers = { b_hgy = { province = 69 } } } }"
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CK3RegionMapperTests/LoadingBrokenRegionWillThrowException.txt";
			void action() => mapper.LoadRegions(landedTitles, regionPath, islandRegionPath);
			KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(action);
			Assert.Equal("Region's test_region2 region test_region does not exist!", exception.Message);
		}
		[Fact]
		public void LoadingBrokenDuchyWillThrowException() {
			var mapper = new CK3RegionMapper();
			var landedTitles = new Title.LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"k_anglia = { d_broken_aquitane = { c_mers = { b_hgy = { province = 69 } } } }"
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CK3RegionMapperTests/LoadingBrokenDuchyWillThrowException.txt";
			void action() => mapper.LoadRegions(landedTitles, regionPath, islandRegionPath);
			KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(action);
			Assert.Equal("Region's test_region duchy d_aquitane does not exist!", exception.Message);
		}
		[Fact]
		public void LoadingBrokenCountyWillThrowException() {
			var mapper = new CK3RegionMapper();
			var landedTitles = new Title.LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"k_anglia = { d_aquitane = { c_mers_broken = { b_hgy = { province = 69 } } } } \n"
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CK3RegionMapperTests/LoadingBrokenCountyWillThrowException.txt";
			void action() => mapper.LoadRegions(landedTitles, regionPath, islandRegionPath);
			KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(action);
			Assert.Equal("Region's test_region county c_mers does not exist!", exception.Message);
		}

		[Fact]
		public void LocationServicesWork() {
			var mapper = new CK3RegionMapper();
			var landedTitles = new Title.LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"d_aquitane = { c_mers = { b_hgy = { province = 69 } } }"
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CK3RegionMapperTests/LocationServicesWork.txt";
			mapper.LoadRegions(landedTitles, regionPath, islandRegionPath);

			Assert.True(mapper.ProvinceIsInRegion(69, "c_mers"));
			Assert.True(mapper.ProvinceIsInRegion(69, "d_aquitane"));
			Assert.True(mapper.ProvinceIsInRegion(69, "test_region"));
			Assert.True(mapper.ProvinceIsInRegion(69, "test_region_bigger"));
			Assert.True(mapper.ProvinceIsInRegion(69, "test_region_biggest"));
		}

		[Fact]
		public void LocationServicesCorrectlyFail() {
			var mapper = new CK3RegionMapper();
			var landedTitles = new Title.LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"d_testduchy = { 1 2 3 } \n" +
				"d_testduchy2 = { 4 5 6 } "
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CK3RegionMapperTests/LocationServicesCorrectlyFail.txt";
			mapper.LoadRegions(landedTitles, regionPath, islandRegionPath);

			Assert.False(mapper.ProvinceIsInRegion(4, "d_testduchy")); // province in different duchy
			Assert.False(mapper.ProvinceIsInRegion(9, "d_testduchy")); // province missing completely
			Assert.False(mapper.ProvinceIsInRegion(5, "test_region")); // province in different region
		}

		[Fact]
		public void LocationServicesFailForNonsense() {
			var mapper = new CK3RegionMapper();
			var landedTitles = new Title.LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } } \n" +
				"k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } }"
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CK3RegionMapperTests/LocationServicesFailForNonsense.txt";

			mapper.LoadRegions(landedTitles, regionPath, islandRegionPath);

			Assert.False(mapper.ProvinceIsInRegion(1, "nonsense"));
			Assert.False(mapper.ProvinceIsInRegion(6, "test_superregion"));
		}

		[Fact]
		public void CorrectParentLocationsReported() {
			var mapper = new CK3RegionMapper();
			var landedTitles = new Title.LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } } \n" +
				"k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n"
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CK3RegionMapperTests/CorrectParentLocationsReported.txt";

			mapper.LoadRegions(landedTitles, regionPath, islandRegionPath);

			Assert.Equal("c_athens", mapper.GetParentCountyName(79));
			Assert.Equal("d_wakaba", mapper.GetParentDuchyName(79));
			Assert.Equal("test_region", mapper.GetParentRegionName(79));
			Assert.Equal("c_defff", mapper.GetParentCountyName(6));
			Assert.Equal("d_hujhu", mapper.GetParentDuchyName(6));
			Assert.Equal("test_region2", mapper.GetParentRegionName(6));
		}

		[Fact]
		public void WrongParentLocationsReturnNull() {
			var mapper = new CK3RegionMapper();
			var landedTitles = new Title.LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"d_testduchy = { 1 2 3 } \n" +
				"d_testduchy2 = { 4 5 6 } "
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CK3RegionMapperTests/WrongParentLocationsReturnNull.txt";

			mapper.LoadRegions(landedTitles, regionPath, islandRegionPath);

			Assert.Null(mapper.GetParentCountyName(7));
			Assert.Null(mapper.GetParentDuchyName(7));
			Assert.Null(mapper.GetParentRegionName(7));
		}

		[Fact]
		public void LocationNameValidationWorks() {
			var mapper = new CK3RegionMapper();
			var landedTitles = new Title.LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } } \n" +
				"k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n" +
				"c_county = { b_barony = { province = 69 } } \n"
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CK3RegionMapperTests/LocationNameValidationWorks.txt";

			mapper.LoadRegions(landedTitles, regionPath, islandRegionPath);

			Assert.True(mapper.RegionNameIsValid("d_wakaba"));
			Assert.True(mapper.RegionNameIsValid("test_region2"));
			Assert.True(mapper.RegionNameIsValid("test_region3"));
			Assert.True(mapper.RegionNameIsValid("c_county"));
			Assert.False(mapper.RegionNameIsValid("nonsense"));
		}

		[Fact]
		public void LocationServicesSucceedsForProvinceField() {
			var mapper = new CK3RegionMapper();
			var landedTitles = new Title.LandedTitles();
			const string regionPath = "TestFiles/regions/CK3RegionMapperTests/LocationServicesSucceedsForProvinceField.txt";

			mapper.LoadRegions(landedTitles, regionPath, islandRegionPath);

			Assert.True(mapper.ProvinceIsInRegion(69, "test_region"));
		}

		[Fact]
		public void LocationServicesSucceedsForCountyField() {
			var mapper = new CK3RegionMapper();
			var landedTitles = new Title.LandedTitles();
			var landedTitlesReader = new BufferedReader(
				"c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } }"
			);
			landedTitles.LoadTitles(landedTitlesReader);
			const string regionPath = "TestFiles/regions/CK3RegionMapperTests/LocationServicesSucceedsForCountyField.txt";

			mapper.LoadRegions(landedTitles, regionPath, islandRegionPath);

			Assert.True(mapper.ProvinceIsInRegion(79, "test_region"));
		}
	}
}
