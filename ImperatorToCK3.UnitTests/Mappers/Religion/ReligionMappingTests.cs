using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Religion;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ReligionMappingTests {
	private const string ck3Path = "TestFiles/regions/ReligionMappingTests";

	[Fact]
	public void RegularMatchOnSimpleReligion() {
		var reader = new BufferedReader("ck3 = flemish imp = dutch");
		var mapping = ReligionMapping.Parse(reader);

		mapping.CK3RegionMapper = new CK3RegionMapper();
		mapping.ImperatorRegionMapper = new ImperatorRegionMapper();

		Assert.Equal("flemish", mapping.Match("dutch", 0, 0));
	}

	[Fact]
	public void MatchOnProvince() {
		var reader = new BufferedReader("ck3 = dutch imp = german ck3Province = 17");
		var mapping = ReligionMapping.Parse(reader);

		mapping.CK3RegionMapper = new CK3RegionMapper();
		mapping.ImperatorRegionMapper = new ImperatorRegionMapper();

		Assert.Equal("dutch", mapping.Match("german", 17, 0));
	}

	[Fact]
	public void MatchOnProvinceFailsForWrongProvince() {
		var reader = new BufferedReader("ck3 = dutch imp = german ck3Province = 17");
		var mapping = ReligionMapping.Parse(reader);

		mapping.CK3RegionMapper = new CK3RegionMapper();
		mapping.ImperatorRegionMapper = new ImperatorRegionMapper();

		Assert.Null(mapping.Match("german", 19, 0));
	}

	[Fact]
	public void MatchOnProvinceFailsForNoProvince() {
		var reader = new BufferedReader("ck3 = dutch imp = german ck3Province = 17");
		var mapping = ReligionMapping.Parse(reader);

		mapping.CK3RegionMapper = new CK3RegionMapper();
		mapping.ImperatorRegionMapper = new ImperatorRegionMapper();

		Assert.Null(mapping.Match("german", 0, 0));
	}

	[Fact]
	public void MatchOnRegion() {
		var theMapper = new CK3RegionMapper();
		var landedTitles = new Title.LandedTitles();
		var landedTitlesReader = new BufferedReader(
			"k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } } } } \n" +
			"k_ghef = { d_hujhu = { c_defff = { b_newbarony2 = { province = 4 } } } } \n"
		);
		landedTitles.LoadTitles(landedTitlesReader);
		theMapper.LoadRegions(landedTitles, ck3Path);

		var reader = new BufferedReader("ck3 = dutch imp = german ck3Region = test_region1");
		var mapping = ReligionMapping.Parse(reader);
		mapping.CK3RegionMapper = theMapper;
		mapping.ImperatorRegionMapper = new ImperatorRegionMapper();

		Assert.Equal("dutch", mapping.Match("german", 4, 0));
	}

	[Fact]
	public void MatchOnRegionFailsForWrongRegion() {
		var theMapper = new CK3RegionMapper();
		var landedTitles = new Title.LandedTitles();
		var landedTitlesReader = new BufferedReader(
			"k_ugada = { d_wakaba = { } } \n" +
			"k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } } } } \n"
		);
		landedTitles.LoadTitles(landedTitlesReader);
		theMapper.LoadRegions(landedTitles, ck3Path);

		var reader = new BufferedReader("ck3 = dutch imp = german ck3Region = test_region1");
		var mapping = ReligionMapping.Parse(reader);
		mapping.CK3RegionMapper = theMapper;
		mapping.ImperatorRegionMapper = new ImperatorRegionMapper();

		Assert.Null(mapping.Match("german", 79, 0));
	}

	[Fact]
	public void MatchOnRegionFailsForNoRegion() {
		var ck3Mapper = new CK3RegionMapper();
		var landedTitles = new Title.LandedTitles();
		var landedTitlesReader = new BufferedReader(
			"k_ugada = { d_wakaba = { } } \n" +
			"k_ghef = { d_hujhu = { } } \n"
		);
		landedTitles.LoadTitles(landedTitlesReader);
		ck3Mapper.LoadRegions(landedTitles, ck3Path);

		var reader = new BufferedReader("ck3 = dutch imp = german ck3Region = test_region3");
		var mapping = ReligionMapping.Parse(reader);
		mapping.CK3RegionMapper = ck3Mapper;
		mapping.ImperatorRegionMapper = new ImperatorRegionMapper();

		Assert.Null(mapping.Match("german", 17, 0));
	}

	[Fact]
	public void MatchOnRegionFailsForNoProvince() {
		var ck3Mapper = new CK3RegionMapper();
		var landedTitles = new Title.LandedTitles();
		var landedTitlesReader = new BufferedReader(
			"k_ugada = { d_wakaba = { } } \n" +
			"k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n"
		);
		landedTitles.LoadTitles(landedTitlesReader);
		ck3Mapper.LoadRegions(landedTitles, ck3Path);

		var reader = new BufferedReader("ck3 = dutch imp = german ck3Region = d_hujhu");
		var mapping = ReligionMapping.Parse(reader);
		mapping.CK3RegionMapper = ck3Mapper;
		mapping.ImperatorRegionMapper = new ImperatorRegionMapper();

		Assert.Null(mapping.Match("german", 0, 0));
	}
}