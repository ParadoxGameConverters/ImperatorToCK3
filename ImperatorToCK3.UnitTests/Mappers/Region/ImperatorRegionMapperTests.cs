using System.Collections.Generic;
using commonItems;
using ImperatorToCK3.Mappers.Region;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Region {
	public class ImperatorRegionMapperTests {
		[Fact]
		public void RegionMapperCanBeEnabled() {
			// We start humble, it's a machine.
			var theMapper = new ImperatorRegionMapper();
			var areaReader = new BufferedReader(string.Empty);
			var regionReader = new BufferedReader(string.Empty);

			theMapper.LoadRegions(areaReader, regionReader);
			Assert.False(theMapper.ProvinceIsInRegion(1, "test"));
			Assert.False(theMapper.RegionNameIsValid("test"));
			Assert.Null(theMapper.GetParentAreaName(1));
			Assert.Null(theMapper.GetParentRegionName(1));
		}

		[Fact]
		public void LoadingBrokenAreaWillThrowException() {
			var theMapper = new ImperatorRegionMapper();
			var areaReader = new BufferedReader(string.Empty);
			var regionReader = new BufferedReader(
				"test_region = { areas = { testarea } }"
			);
			Assert.Throws<KeyNotFoundException>(() => theMapper.LoadRegions(areaReader, regionReader));
		}

		[Fact]
		public void LocationServicesWork() {
			var theMapper = new ImperatorRegionMapper();
			var areaReader = new BufferedReader("test_area = { provinces = { 1 2 3 } }\n");
			var regionReader = new BufferedReader("test_region = { areas = { test_area } }\n");
			theMapper.LoadRegions(areaReader, regionReader);

			Assert.True(theMapper.ProvinceIsInRegion(3, "test_area"));
			Assert.True(theMapper.ProvinceIsInRegion(3, "test_region"));
		}

		[Fact]
		public void LocationServicesCorrectlyFail() {
			var theMapper = new ImperatorRegionMapper();
			var areaReader = new BufferedReader(
				"test_area = { provinces = { 1 2 3 } }\n" +
				"test_area2 = { provinces = { 4 5 6 } }\n" +
				"test_area3 = { provinces = { 7 8 9 } }\n"
			);
			var regionReader = new BufferedReader(
				"test_region = { areas = { test_area test_area2 } }\n" +
				"test_region2 = { areas = { test_area3 } }\n"
			);
			theMapper.LoadRegions(areaReader, regionReader);

			Assert.False(theMapper.ProvinceIsInRegion(3, "test_area2")); // province in different area
			Assert.False(theMapper.ProvinceIsInRegion(9, "test_region")); // province in different region
			Assert.False(theMapper.ProvinceIsInRegion(9, "test_region")); // province missing completely
		}

		[Fact]
		public void LocationServicesFailForNonsense() {
			var theMapper = new ImperatorRegionMapper();
			var areaReader = new BufferedReader(
			"test1 = { provinces = { 1 2 3 } }"
			);
			var regionReader = new BufferedReader(
			"test_region = { areas = { test1 } }"
			);
			theMapper.LoadRegions(areaReader, regionReader);

			Assert.False(theMapper.ProvinceIsInRegion(1, "nonsense"));
		}

		[Fact]
		public void CorrectParentLocationsReported() {
			var theMapper = new ImperatorRegionMapper();
			var areaReader = new BufferedReader(
				"test_area = { provinces = { 1 2 3 } }\n" +
				"test_area2 = { provinces = { 4 5 6 } }\n"
			);
			var regionReader = new BufferedReader(
				"test_region = { areas = { test_area } }\n" +
				"test_region2 = { areas = { test_area2 } }\n"
			);
			theMapper.LoadRegions(areaReader, regionReader);

			Assert.Equal("test_area", theMapper.GetParentAreaName(2));
			Assert.Equal("test_region", theMapper.GetParentRegionName(2));
			Assert.Equal("test_area2", theMapper.GetParentAreaName(5));
			Assert.Equal("test_region2", theMapper.GetParentRegionName(5));
		}

		[Fact]
		public void WrongParentLocationsReturnNullopt() {
			var theMapper = new ImperatorRegionMapper();

			var areaReader = new BufferedReader(
			"test_area = { provinces = { 1 2 3 } }\n"
			);
			var regionReader = new BufferedReader(
			"test_region = { areas = { test_area } }\n"
			);
			theMapper.LoadRegions(areaReader, regionReader);

			Assert.Null(theMapper.GetParentAreaName(5));
			Assert.Null(theMapper.GetParentRegionName(5));
		}

		[Fact]
		public void LocationNameValidationWorks() {
			var theMapper = new ImperatorRegionMapper();

			var areaReader = new BufferedReader(
				"test_area = { provinces = { 1 2 3 } }" +
				"test_area2 = { provinces = { 4 5 6 } }\n"
			);
			var regionReader = new BufferedReader(
				"test_region = { areas = { test_area } }" +
				"test_region2 = { areas = { test_area2 } }\n"
			);
			theMapper.LoadRegions(areaReader, regionReader);

			Assert.True(theMapper.RegionNameIsValid("test_area"));
			Assert.True(theMapper.RegionNameIsValid("test_area2"));
			Assert.True(theMapper.RegionNameIsValid("test_region"));
			Assert.True(theMapper.RegionNameIsValid("test_region2"));
			Assert.False(theMapper.RegionNameIsValid("nonsense"));
		}
	}
}
