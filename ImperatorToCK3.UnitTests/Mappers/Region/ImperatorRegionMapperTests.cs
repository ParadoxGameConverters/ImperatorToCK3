using commonItems;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Region;

public class ImperatorRegionMapperTests {
	[Fact]
	public void RegionMapperCanBeEnabled() {
		// We start humble, it's a machine.
		var theMapper = new ImperatorRegionMapper();

		Assert.False(theMapper.ProvinceIsInRegion(1, "test"));
		Assert.False(theMapper.RegionNameIsValid("test"));
		Assert.Null(theMapper.GetParentAreaName(1));
		Assert.Null(theMapper.GetParentRegionName(1));
	}

	[Fact]
	public void LoadingBrokenAreaWillThrowException() {
		var config = new Configuration { ImperatorPath = "TestFiles/ImperatorRegionMapper/test1" };
		var mods = new List<Mod>();
		Assert.Throws<KeyNotFoundException>(() => _ = new ImperatorRegionMapper(config, mods));
	}

	[Fact]
	public void LocationServicesWork() {
		var config = new Configuration { ImperatorPath = "TestFiles/ImperatorRegionMapper/test2" };
		var mods = new List<Mod>();
		var theMapper = new ImperatorRegionMapper(config, mods);

		Assert.True(theMapper.ProvinceIsInRegion(3, "test_area"));
		Assert.True(theMapper.ProvinceIsInRegion(3, "test_region"));
	}

	[Fact]
	public void LocationServicesCorrectlyFail() {
		var config = new Configuration { ImperatorPath = "TestFiles/ImperatorRegionMapper/test3" };
		var mods = new List<Mod>();
		var theMapper = new ImperatorRegionMapper(config, mods);

		Assert.False(theMapper.ProvinceIsInRegion(3, "test_area2")); // province in different area
		Assert.False(theMapper.ProvinceIsInRegion(9, "test_region")); // province in different region
		Assert.False(theMapper.ProvinceIsInRegion(9, "test_region")); // province missing completely
	}

	[Fact]
	public void LocationServicesFailForNonsense() {
		var config = new Configuration { ImperatorPath = "TestFiles/ImperatorRegionMapper/test4" };
		var mods = new List<Mod>();
		var theMapper = new ImperatorRegionMapper(config, mods);

		Assert.False(theMapper.ProvinceIsInRegion(1, "nonsense"));
	}

	[Fact]
	public void CorrectParentLocationsReported() {
		var config = new Configuration { ImperatorPath = "TestFiles/ImperatorRegionMapper/test5" };
		var mods = new List<Mod>();
		var theMapper = new ImperatorRegionMapper(config, mods);

		Assert.Equal("test_area", theMapper.GetParentAreaName(2));
		Assert.Equal("test_region", theMapper.GetParentRegionName(2));
		Assert.Equal("test_area2", theMapper.GetParentAreaName(5));
		Assert.Equal("test_region2", theMapper.GetParentRegionName(5));
	}

	[Fact]
	public void WrongParentLocationsReturnNull() {
		var config = new Configuration { ImperatorPath = "TestFiles/ImperatorRegionMapper/test6" };
		var mods = new List<Mod>();
		var theMapper = new ImperatorRegionMapper(config, mods);

		Assert.Null(theMapper.GetParentAreaName(5));
		Assert.Null(theMapper.GetParentRegionName(5));
	}

	[Fact]
	public void LocationNameValidationWorks() {
		var config = new Configuration { ImperatorPath = "TestFiles/ImperatorRegionMapper/test7" };
		var mods = new List<Mod>();
		var theMapper = new ImperatorRegionMapper(config, mods);

		Assert.True(theMapper.RegionNameIsValid("test_area"));
		Assert.True(theMapper.RegionNameIsValid("test_area2"));
		Assert.True(theMapper.RegionNameIsValid("test_region"));
		Assert.True(theMapper.RegionNameIsValid("test_region2"));
		Assert.False(theMapper.RegionNameIsValid("nonsense"));
	}
}