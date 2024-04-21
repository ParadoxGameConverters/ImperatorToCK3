using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using System.Collections.Generic;
using System.IO;
using Xunit;
using System;

namespace ImperatorToCK3.UnitTests.Mappers.Religion;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ReligionMappingTests {
	private const string ImperatorRoot = "TestFiles/Imperator/root";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly MapData irMapData = new(irModFS);
	private static readonly AreaCollection areas = new();
	private static readonly ImperatorRegionMapper irRegionMapper = new(areas, irMapData);
	private const string ck3Path = "TestFiles/regions/ReligionMappingTests";
	private string CK3Root => Path.Combine(ck3Path, "game");
	
	public ReligionMappingTests() {
		irRegionMapper.LoadRegions(irModFS, new ColorFactory());
	}

	[Fact]
	public void RegularMatchOnSimpleReligion() {
		var reader = new BufferedReader("ck3 = flemish ir = dutch");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Equal("flemish", mapping.Match("dutch", null, null, null, null, new Configuration(), irRegionMapper, new CK3RegionMapper()));
	}

	[Fact]
	public void MatchOnProvince() {
		var reader = new BufferedReader("ck3 = dutch ir = german ck3Province = 17");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Equal("dutch", mapping.Match("german", null, 17, null, null, new Configuration(), irRegionMapper, new CK3RegionMapper()));
	}

	[Fact]
	public void MatchOnProvinceFailsForWrongProvince() {
		var reader = new BufferedReader("ck3 = dutch ir = german ck3Province = 17");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Null(mapping.Match("german", null, 19, null, null, new Configuration(), irRegionMapper, new CK3RegionMapper()));
	}

	[Fact]
	public void MatchOnProvinceFailsForNoProvince() {
		var reader = new BufferedReader("ck3 = dutch ir = german ck3Province = 17");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Null(mapping.Match("german", null, null, null, null, new Configuration(), irRegionMapper, new CK3RegionMapper()));
	}

	[Fact]
	public void MatchOnRegion() {
		var ck3RegionMapper = new CK3RegionMapper();
		var landedTitles = new Title.LandedTitles();
		var landedTitlesReader = new BufferedReader(
			"k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } } } } \n" +
			"k_ghef = { d_hujhu = { c_defff = { b_newbarony2 = { province = 4 } } } } \n"
		);
		landedTitles.LoadTitles(landedTitlesReader);

		var ck3ModFS = new ModFilesystem(CK3Root, new List<Mod>());
		ck3RegionMapper.LoadRegions(ck3ModFS, landedTitles);

		var reader = new BufferedReader("ck3 = dutch ir = german ck3Region = test_region1");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Equal("dutch", mapping.Match("german", null, 4, null, null, new Configuration(), irRegionMapper, ck3RegionMapper));
	}

	[Fact]
	public void MatchOnRegionFailsForWrongRegion() {
		var ck3RegionMapper = new CK3RegionMapper();
		var landedTitles = new Title.LandedTitles();
		var landedTitlesReader = new BufferedReader(
			"k_ugada = { d_wakaba = { } } \n" +
			"k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } } } } \n"
		);
		landedTitles.LoadTitles(landedTitlesReader);

		var ck3ModFS = new ModFilesystem(CK3Root, new List<Mod>());
		ck3RegionMapper.LoadRegions(ck3ModFS, landedTitles);

		var reader = new BufferedReader("ck3 = dutch ir = german ck3Region = test_region1");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Null(mapping.Match("german", null, 79, null, null, new Configuration(), irRegionMapper, ck3RegionMapper));
	}

	[Fact]
	public void MatchOnRegionFailsForNoRegion() {
		var ck3RegionMapper = new CK3RegionMapper();
		var landedTitles = new Title.LandedTitles();
		var landedTitlesReader = new BufferedReader(
			"k_ugada = { d_wakaba = { } } \n" +
			"k_ghef = { d_hujhu = { } } \n"
		);
		landedTitles.LoadTitles(landedTitlesReader);

		var ck3ModFS = new ModFilesystem(CK3Root, new List<Mod>());
		ck3RegionMapper.LoadRegions(ck3ModFS, landedTitles);

		var reader = new BufferedReader("ck3 = dutch ir = german ck3Region = test_region3");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Null(mapping.Match("german", null, 17, null, null, new Configuration(), irRegionMapper, ck3RegionMapper));
	}

	[Fact]
	public void MatchOnRegionFailsForNoProvince() {
		var ck3RegionMapper = new CK3RegionMapper();
		var landedTitles = new Title.LandedTitles();
		var landedTitlesReader = new BufferedReader(
			"k_ugada = { d_wakaba = { } } \n" +
			"k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n"
		);
		landedTitles.LoadTitles(landedTitlesReader);

		var ck3ModFS = new ModFilesystem(CK3Root, new List<Mod>());
		ck3RegionMapper.LoadRegions(ck3ModFS, landedTitles);

		var reader = new BufferedReader("ck3 = dutch ir = german ck3Region = d_hujhu");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Null(mapping.Match("german", null, null, null, null, new Configuration(), irRegionMapper, ck3RegionMapper));
	}

	[Fact]
	public void HeresiesInHistoricalAreasValueCorrectlyMatchesYes() {
		var reader = new BufferedReader("ck3=dutch ir=german heresiesInHistoricalAreas=yes");
		var mapping = ReligionMapping.Parse(reader);
		var config = new Configuration { HeresiesInHistoricalAreas = true };

		Assert.Equal("dutch", mapping.Match("german", null, null, null, null, config, irRegionMapper, new CK3RegionMapper()));
	}

	[Fact]
	public void HeresiesInHistoricalAreasValueCorrectlyMismatchesYes() {
		var reader = new BufferedReader("ck3=dutch ir=german heresiesInHistoricalAreas=yes");
		var mapping = ReligionMapping.Parse(reader);
		var config = new Configuration { HeresiesInHistoricalAreas = false };

		Assert.Null(mapping.Match("german", null, null, null, null, config, irRegionMapper, new CK3RegionMapper()));
	}

	[Fact]
	public void HeresiesInHistoricalAreasValueCorrectlyMatchesNo() {
		var reader = new BufferedReader("ck3=dutch ir=german heresiesInHistoricalAreas=no");
		var mapping = ReligionMapping.Parse(reader);
		var config = new Configuration { HeresiesInHistoricalAreas = false };

		Assert.Equal("dutch", mapping.Match("german", null, null, null, null, config, irRegionMapper, new CK3RegionMapper()));
	}

	[Fact]
	public void HeresiesInHistoricalAreasValueCorrectlyMismatchesNo() {
		var reader = new BufferedReader("ck3=dutch ir=german heresiesInHistoricalAreas=no");
		var mapping = ReligionMapping.Parse(reader);
		var config = new Configuration { HeresiesInHistoricalAreas = true };

		Assert.Null(mapping.Match("german", null, null, null, null, config, irRegionMapper, new CK3RegionMapper()));
	}

	[Fact]
	public void DateIsCorrectlyUsedAsTrigger() {
		var orthodoxReader = new BufferedReader("ck3=orthodox ir=christian date_gte=1054.7.16");
		var orthodoxMapping = ReligionMapping.Parse(orthodoxReader);
		
		var chalcedonianReader = new BufferedReader("ck3=chalcedonian ir=christian");
		var chalcedonianMapping = ReligionMapping.Parse(chalcedonianReader);
		
		// date after the schism
		var config = new Configuration { CK3BookmarkDate = new Date(1066, 9, 15) };
		Assert.Equal("orthodox", orthodoxMapping.Match("christian", null, null, null, null, config, irRegionMapper, new CK3RegionMapper()));
		// fallback to chalcedonian should also work
		Assert.Equal("chalcedonian", chalcedonianMapping.Match("christian", null, null, null, null, config, irRegionMapper, new CK3RegionMapper()));
		
		// date of the schism
		config = new Configuration { CK3BookmarkDate = new Date(1054, 7, 16) };
		Assert.Equal("orthodox", orthodoxMapping.Match("christian", null, null, null, null, config, irRegionMapper, new CK3RegionMapper()));
		Assert.Equal("chalcedonian", chalcedonianMapping.Match("christian", null, null, null, null, config, irRegionMapper, new CK3RegionMapper()));
		
		// date before the schism
		config = new Configuration { CK3BookmarkDate = new Date(1000, 0, 0) };
		Assert.Null(orthodoxMapping.Match("christian", null, null, null, null, config, irRegionMapper, new CK3RegionMapper()));
		Assert.Equal("chalcedonian", chalcedonianMapping.Match("christian", null, null, null, null, config, irRegionMapper, new CK3RegionMapper()));
	}
	
	[Theory]
	[InlineData("roman", "orthodox")]
	[InlineData("greek", "orthodox")]
	[InlineData("briton", null)]
	[InlineData("armenian", null)]
	[InlineData(null, null)]
	public void MappingWithSpecifiedCK3CulturesCorrectlyMatches(string? ck3CultureId, string? expectedMatchedFaith) {
		var reader = new BufferedReader(
			"ck3=orthodox ir=christian ir=nicene ir=orthodox ck3Culture=roman ck3Culture=greek"
		);
		var mapping = ReligionMapping.Parse(reader);

		Assert.Equal(expectedMatchedFaith, mapping.Match("nicene", ck3CultureId, 56, 49, null, new Configuration(), irRegionMapper, new CK3RegionMapper()));
	}
}