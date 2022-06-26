using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Religion;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ReligionMappingTests {
	private const string ck3Path = "TestFiles/regions/ReligionMappingTests";
	private string CK3Root => Path.Combine(ck3Path, "game");

	[Fact]
	public void RegularMatchOnSimpleReligion() {
		var reader = new BufferedReader("ck3 = flemish imp = dutch");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Equal("flemish", mapping.Match("dutch", 0, 0, new Configuration(), new ImperatorRegionMapper(), new CK3RegionMapper()));
	}

	[Fact]
	public void MatchOnProvince() {
		var reader = new BufferedReader("ck3 = dutch imp = german ck3Province = 17");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Equal("dutch", mapping.Match("german", 17, 0, new Configuration(), new ImperatorRegionMapper(), new CK3RegionMapper()));
	}

	[Fact]
	public void MatchOnProvinceFailsForWrongProvince() {
		var reader = new BufferedReader("ck3 = dutch imp = german ck3Province = 17");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Null(mapping.Match("german", 19, 0, new Configuration(), new ImperatorRegionMapper(), new CK3RegionMapper()));
	}

	[Fact]
	public void MatchOnProvinceFailsForNoProvince() {
		var reader = new BufferedReader("ck3 = dutch imp = german ck3Province = 17");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Null(mapping.Match("german", 0, 0, new Configuration(), new ImperatorRegionMapper(), new CK3RegionMapper()));
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

		var reader = new BufferedReader("ck3 = dutch imp = german ck3Region = test_region1");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Equal("dutch", mapping.Match("german", 4, 0, new Configuration(), new ImperatorRegionMapper(), ck3RegionMapper));
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

		var reader = new BufferedReader("ck3 = dutch imp = german ck3Region = test_region1");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Null(mapping.Match("german", 79, 0, new Configuration(), new ImperatorRegionMapper(), ck3RegionMapper));
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

		var reader = new BufferedReader("ck3 = dutch imp = german ck3Region = test_region3");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Null(mapping.Match("german", 17, 0, new Configuration(), new ImperatorRegionMapper(), ck3RegionMapper));
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

		var reader = new BufferedReader("ck3 = dutch imp = german ck3Region = d_hujhu");
		var mapping = ReligionMapping.Parse(reader);

		Assert.Null(mapping.Match("german", 0, 0, new Configuration(), new ImperatorRegionMapper(), ck3RegionMapper));
	}

	[Fact]
	public void HeresiesInHistoricalAreasValueCorrectlyMatchesYes() {
		var reader = new BufferedReader("ck3=dutch imp=german heresiesInHistoricalAreas=yes");
		var mapping = ReligionMapping.Parse(reader);
		var config = new Configuration { HeresiesInHistoricalAreas = true };

		Assert.Equal("dutch", mapping.Match("german", 0, 0, config, new ImperatorRegionMapper(), new CK3RegionMapper()));
	}

	[Fact]
	public void HeresiesInHistoricalAreasValueCorrectlyMismatchesYes() {
		var reader = new BufferedReader("ck3=dutch imp=german heresiesInHistoricalAreas=yes");
		var mapping = ReligionMapping.Parse(reader);
		var config = new Configuration { HeresiesInHistoricalAreas = false };

		Assert.Null(mapping.Match("german", 0, 0, config, new ImperatorRegionMapper(), new CK3RegionMapper()));
	}

	[Fact]
	public void HeresiesInHistoricalAreasValueCorrectlyMatchesNo() {
		var reader = new BufferedReader("ck3=dutch imp=german heresiesInHistoricalAreas=no");
		var mapping = ReligionMapping.Parse(reader);
		var config = new Configuration { HeresiesInHistoricalAreas = false };

		Assert.Equal("dutch", mapping.Match("german", 0, 0, config, new ImperatorRegionMapper(), new CK3RegionMapper()));
	}

	[Fact]
	public void HeresiesInHistoricalAreasValueCorrectlyMismatchesNo() {
		var reader = new BufferedReader("ck3=dutch imp=german heresiesInHistoricalAreas=no");
		var mapping = ReligionMapping.Parse(reader);
		var config = new Configuration { HeresiesInHistoricalAreas = true };

		Assert.Null(mapping.Match("german", 0, 0, config, new ImperatorRegionMapper(), new CK3RegionMapper()));
	}
}