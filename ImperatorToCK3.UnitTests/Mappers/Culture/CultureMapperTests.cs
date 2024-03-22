using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Mappers.Culture;
using Xunit;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;

namespace ImperatorToCK3.UnitTests.Mappers.Culture;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CultureMapperTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly MapData irMapData = new(irModFS);
	private static readonly ImperatorRegionMapper irRegionMapper;
	private static readonly TestCK3CultureCollection cultures = new();
	
	static CultureMapperTests() {
		var irProvinces = new ProvinceCollection {new(1), new(2), new(3)};
		AreaCollection areas = new();
		areas.LoadAreas(irModFS, irProvinces);
		irRegionMapper = new ImperatorRegionMapper(areas, irMapData);
		irRegionMapper.LoadRegions(irModFS, new ColorFactory());
		
		cultures.GenerateTestCulture("culture");
		cultures.GenerateTestCulture("low_germ");
		cultures.GenerateTestCulture("high_germ");
	}

	[Fact]
	public void NonMatchGivesEmptyOptional() {
		var reader = new BufferedReader(
			"link = { ck3 = culture ir = culture }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper(), cultures);

		Assert.Null(culMapper.Match("nonMatchingCulture", 56, 49, "ROM"));
	}

	[Fact]
	public void SimpleCultureMatches() {
		var reader = new BufferedReader(
			"link = { ck3 = culture ir = test }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper(), cultures);

		Assert.Equal("culture", culMapper.Match("test", 56, 49, "ROM"));
	}

	[Fact]
	public void SimpleCultureCorrectlyMatches() {
		var reader = new BufferedReader(
			"link = { ck3 = culture ir = qwe ir = test ir = poi }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper(), cultures);

		Assert.Equal("culture", culMapper.Match("test", 56, 49, "ROM"));
	}

	[Fact]
	public void CultureMatchesWithCapital() {
		var reader = new BufferedReader(
			"link = { ck3 = culture ir = qwe ir = test ir = poi religion = thereligion ck3Province = 4 }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper(), cultures);

		Assert.Equal("culture", culMapper.Match("test", 4, 49, "ROM"));
	}

	[Fact]
	public void CultureFailsWithWrongCapital() {
		var reader = new BufferedReader(
			"link = { ck3 = culture ir = qwe ir = test ir = poi religion = thereligion ck3Province = 4 }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper(), cultures);

		Assert.Null(culMapper.Match("test", 3, 49, "ROM"));
	}

	[Fact]
	public void CultureMatchesWithHistoricalTag() {
		var reader = new BufferedReader(
			"link = { ck3 = culture ir = qwe ir = test ir = poi religion = thereligion ck3Province = 4 tag = ROM }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper(), cultures);

		Assert.Equal("culture", culMapper.Match("test", 4, 49, "ROM"));
	}

	[Fact]
	public void CultureFailsWithWrongHistoricalTag() {
		var reader = new BufferedReader(
			"link = { ck3 = culture ir = qwe ir = test ir = poi religion = thereligion ck3Province = 4 historicalTag = ROM }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper(), cultures);

		Assert.Null(culMapper.Match("test", 4, 49, "WRO"));
	}

	[Fact]
	public void CultureMatchesWithNoHistoricalTag() {
		var reader = new BufferedReader(
			"link = { ck3 = culture ir = qwe ir = test ir = poi religion = thereligion ck3Province = 4 }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper(), cultures);

		Assert.Equal("culture", culMapper.Match("test", 4, 49, null));
	}

	[Fact]
	public void CultureMatchesWithNoHistoricalTagInRule() {
		var reader = new BufferedReader(
			"link = { ck3 = culture ir = qwe ir = test ir = poi religion = thereligion ck3Province = 4}"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper(), cultures);

		Assert.Equal("culture", culMapper.Match("test", 4, 49, "ROM"));
	}

	[Fact]
	public void CultureFailsWithHistoricalTagInRule() {
		var reader = new BufferedReader("""
		link = {
			ck3=culture
			ir=qwe ir=test ir=poi
			religion=thereligion
			ck3Province=4
			historicalTag=ROM
		}
		""");
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper(), cultures);

		Assert.Null(culMapper.Match("test", 4, 49, null));
	}

	[Fact]
	public void VariablesWorkInLinks() {
		var reader = new BufferedReader(
			"@germ_cultures = \"ir=sennonian ir=bellovacian ir=veliocassian ir=morinian\" \r\n" +
			"link = { ck3=low_germ @germ_cultures irProvince=1}\r\n" +
			"link = { ck3=high_germ @germ_cultures irProvince=2}"
		);
		var cultureMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper(), cultures);

		Assert.Null(cultureMapper.Match("missing_culture", null, irProvinceId: 1, null));
		Assert.Equal("low_germ", cultureMapper.Match("bellovacian", null, irProvinceId: 1, null));
		Assert.Equal("high_germ", cultureMapper.Match("bellovacian", null, irProvinceId: 2, null));
	}
}