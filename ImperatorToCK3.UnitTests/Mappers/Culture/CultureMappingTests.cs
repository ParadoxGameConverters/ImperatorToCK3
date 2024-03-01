using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;
using System.IO;
using System;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Culture;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CultureMappingTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly MapData irMapData = new(irModFS);
	private static readonly ImperatorRegionMapper irRegionMapper;

	static CultureMappingTests() {
		var irProvinces = new ProvinceCollection {new(1), new(2), new(3)};
		AreaCollection areas = new();
		areas.LoadAreas(irModFS, irProvinces);
		irRegionMapper = new ImperatorRegionMapper(areas, irMapData);
		irRegionMapper.LoadRegions(irModFS, new ColorFactory());
	}
	
	[Fact]
	public void MatchOnRegion() {
		var ck3RegionMapper = new CK3RegionMapper();
		var landedTitles = new Title.LandedTitles();
		var landedTitlesReader = new BufferedReader(
			"k_ghef = { d_hujhu = { c_defff = { b_newbarony2 = { province = 4 } } } }"
		);
		landedTitles.LoadTitles(landedTitlesReader);
		const string ck3Path = "TestFiles/regions/CultureMappingTests/MatchOnRegion";
		var ck3Root = Path.Combine(ck3Path, "game");
		var ck3ModFS = new ModFilesystem(ck3Root, new List<Mod>());
		ck3RegionMapper.LoadRegions(ck3ModFS, landedTitles);

		var reader = new BufferedReader(
			"ck3 = dutch ir = german ck3Region = test_region1"
		);
		var theMapping = CultureMappingRule.Parse(reader);

		Assert.Equal("dutch", theMapping.Match("german",  4, null, null, irRegionMapper, ck3RegionMapper));
	}

	[Fact]
	public void MatchOnRegionFailsForWrongRegion() {
		var ck3RegionMapper = new CK3RegionMapper();
		var landedTitles = new Title.LandedTitles();
		var landedTitlesReader = new BufferedReader(
			"k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } } } } \n" +
			"k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } } } } \n"
		);
		landedTitles.LoadTitles(landedTitlesReader);
		const string ck3Path = "TestFiles/regions/CultureMappingTests/MatchOnRegionFailsForWrongRegion";
		var ck3Root = Path.Combine(ck3Path, "game");
		var ck3ModFS = new ModFilesystem(ck3Root, new List<Mod>());
		ck3RegionMapper.LoadRegions(ck3ModFS, landedTitles);

		var reader = new BufferedReader(
			"ck3 = dutch ir = german ck3Region = test_region2"
		);
		var theMapping = CultureMappingRule.Parse(reader);

		Assert.Null(theMapping.Match("german", 79, null, null, irRegionMapper, ck3RegionMapper));
	}

	[Fact]
	public void MatchOnRegionFailsForNoRegion() {
		var ck3RegionMapper = new CK3RegionMapper();
		var landedTitles = new Title.LandedTitles();
		var landedTitlesReader = new BufferedReader(string.Empty);
		landedTitles.LoadTitles(landedTitlesReader);
		const string ck3Path = "TestFiles/regions/CultureMappingTests/empty";
		var ck3Root = Path.Combine(ck3Path, "game");
		var ck3ModFS = new ModFilesystem(ck3Root, new List<Mod>());
		ck3RegionMapper.LoadRegions(ck3ModFS, landedTitles);

		var reader = new BufferedReader(
			"ck3 = dutch ir = german ck3Region = test_region3"
		);
		var theMapping = CultureMappingRule.Parse(reader);

		Assert.Null(theMapping.Match("german", 17, null, null, irRegionMapper, ck3RegionMapper));
	}

	[Fact]
	public void MatchOnRegionFailsForNoProvince() {
		var ck3RegionMapper = new CK3RegionMapper();
		var landedTitles = new Title.LandedTitles();
		var landedTitlesReader = new BufferedReader(
			"k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n"
		);
		landedTitles.LoadTitles(landedTitlesReader);
		const string ck3Path = "TestFiles/regions/CultureMappingTests/empty";
		var ck3Root = Path.Combine(ck3Path, "game");
		var ck3ModFS = new ModFilesystem(ck3Root, new List<Mod>());
		ck3RegionMapper.LoadRegions(ck3ModFS, landedTitles);

		var reader = new BufferedReader(
			"ck3 = dutch ir = german ck3Region = d_hujhu"
		);
		var theMapping = CultureMappingRule.Parse(reader);

		Assert.Null(theMapping.Match("german", null, null, null, irRegionMapper, ck3RegionMapper));
	}
}