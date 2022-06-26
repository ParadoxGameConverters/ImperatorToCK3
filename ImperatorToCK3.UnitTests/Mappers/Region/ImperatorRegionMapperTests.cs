﻿using commonItems.Mods;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;
using System.IO;
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
		const string imperatorPath = "TestFiles/ImperatorRegionMapper/test1";
		var imperatorRoot = Path.Combine(imperatorPath, "game");
		var mods = new List<Mod>();
		var imperatorModFS = new ModFilesystem(imperatorRoot, mods);

		Assert.Throws<KeyNotFoundException>(() => _ = new ImperatorRegionMapper(imperatorModFS));
	}

	[Fact]
	public void LocationServicesWork() {
		const string imperatorPath = "TestFiles/ImperatorRegionMapper/test2";
		var imperatorRoot = Path.Combine(imperatorPath, "game");
		var imperatorModFS = new ModFilesystem(imperatorRoot, new List<Mod>());
		var theMapper = new ImperatorRegionMapper(imperatorModFS);

		Assert.True(theMapper.ProvinceIsInRegion(3, "test_area"));
		Assert.True(theMapper.ProvinceIsInRegion(3, "test_region"));
	}

	[Fact]
	public void LocationServicesCorrectlyFail() {
		const string imperatorPath = "TestFiles/ImperatorRegionMapper/test3";
		var imperatorRoot = Path.Combine(imperatorPath, "game");
		var imperatorModFS = new ModFilesystem(imperatorRoot, new List<Mod>());
		var theMapper = new ImperatorRegionMapper(imperatorModFS);

		Assert.False(theMapper.ProvinceIsInRegion(3, "test_area2")); // province in different area
		Assert.False(theMapper.ProvinceIsInRegion(9, "test_region")); // province in different region
		Assert.False(theMapper.ProvinceIsInRegion(9, "test_region")); // province missing completely
	}

	[Fact]
	public void LocationServicesFailForNonsense() {
		const string imperatorPath = "TestFiles/ImperatorRegionMapper/test4";
		var imperatorRoot = Path.Combine(imperatorPath, "game");
		var imperatorModFS = new ModFilesystem(imperatorRoot, new List<Mod>());
		var theMapper = new ImperatorRegionMapper(imperatorModFS);

		Assert.False(theMapper.ProvinceIsInRegion(1, "nonsense"));
	}

	[Fact]
	public void CorrectParentLocationsReported() {
		const string imperatorPath = "TestFiles/ImperatorRegionMapper/test5";
		var imperatorRoot = Path.Combine(imperatorPath, "game");
		var imperatorModFS = new ModFilesystem(imperatorRoot, new List<Mod>());
		var theMapper = new ImperatorRegionMapper(imperatorModFS);

		Assert.Equal("test_area", theMapper.GetParentAreaName(2));
		Assert.Equal("test_region", theMapper.GetParentRegionName(2));
		Assert.Equal("test_area2", theMapper.GetParentAreaName(5));
		Assert.Equal("test_region2", theMapper.GetParentRegionName(5));
	}

	[Fact]
	public void WrongParentLocationsReturnNull() {
		const string imperatorPath = "TestFiles/ImperatorRegionMapper/test6";
		var imperatorRoot = Path.Combine(imperatorPath, "game");
		var imperatorModFS = new ModFilesystem(imperatorRoot, new List<Mod>());
		var theMapper = new ImperatorRegionMapper(imperatorModFS);

		Assert.Null(theMapper.GetParentAreaName(5));
		Assert.Null(theMapper.GetParentRegionName(5));
	}

	[Fact]
	public void LocationNameValidationWorks() {
		const string imperatorPath = "TestFiles/ImperatorRegionMapper/test7";
		var imperatorRoot = Path.Combine(imperatorPath, "game");
		var imperatorModFS = new ModFilesystem(imperatorRoot, new List<Mod>());
		var theMapper = new ImperatorRegionMapper(imperatorModFS);

		Assert.True(theMapper.RegionNameIsValid("test_area"));
		Assert.True(theMapper.RegionNameIsValid("test_area2"));
		Assert.True(theMapper.RegionNameIsValid("test_region"));
		Assert.True(theMapper.RegionNameIsValid("test_region2"));
		Assert.False(theMapper.RegionNameIsValid("nonsense"));
	}

	[Fact]
	public void ModAreasAndRegionsCanBeLoaded() {
		const string imperatorPath = "TestFiles/ImperatorRegionMapper/test8/CK3";
		var imperatorRoot = Path.Combine(imperatorPath, "game");
		var mods = new List<Mod> { new("mod1", "TestFiles/ImperatorRegionMapper/test8/mod1") };
		var imperatorModFS = new ModFilesystem(imperatorRoot, mods);
		var theMapper = new ImperatorRegionMapper(imperatorModFS);

		Assert.False(theMapper.RegionNameIsValid("vanilla_area")); // present only in vanilla file which is overriden by mod
		Assert.True(theMapper.RegionNameIsValid("common_area"));
		Assert.True(theMapper.RegionNameIsValid("mod_area"));

		Assert.False(theMapper.RegionNameIsValid("vanilla_region")); // present only in vanilla file which is overriden by mod
		Assert.True(theMapper.RegionNameIsValid("common_region"));
		Assert.True(theMapper.RegionNameIsValid("mod_region"));
	}
}