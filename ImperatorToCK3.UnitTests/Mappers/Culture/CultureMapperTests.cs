using commonItems;
using commonItems.Mods;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Mappers.Culture;
using Xunit;
using ImperatorToCK3.Mappers.Region;
using System;

namespace ImperatorToCK3.UnitTests.Mappers.Culture;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CultureMapperTests {
	private const string ImperatorRoot = "TestFiles/Imperator/root";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly AreaCollection areas = new();
	private static readonly ImperatorRegionMapper irRegionMapper = new(irModFS, areas);

	[Fact]
	public void NonMatchGivesEmptyOptional() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = culture }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Null(culMapper.Match("nonMatchingCulture", "", 56, 49, "ROM"));
	}

	[Fact]
	public void SimpleCultureMatches() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = test }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Equal("culture", culMapper.Match("test", "", 56, 49, "ROM"));
	}

	[Fact]
	public void SimpleCultureCorrectlyMatches() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Equal("culture", culMapper.Match("test", "", 56, 49, "ROM"));
	}

	[Fact]
	public void CultureMatchesWithReligion() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Equal("culture", culMapper.Match("test", "thereligion", 56, 49, "ROM"));
	}

	[Fact]
	public void CultureFailsWithWrongReligion() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Null(culMapper.Match("test", "unreligion", 56, 49, "ROM"));
	}

	[Fact]
	public void CultureFailsWithNoReligion() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Null(culMapper.Match("test", "", 56, 49, "ROM"));
	}

	[Fact]
	public void CultureMatchesWithCapital() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4 }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Equal("culture", culMapper.Match("test", "thereligion", 4, 49, "ROM"));
	}

	[Fact]
	public void CultureFailsWithWrongCapital() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4 }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Null(culMapper.Match("test", "thereligion", 3, 49, "ROM"));
	}

	[Fact]
	public void CultureMatchesWithHistoricalTag() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4 tag = ROM }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Equal("culture", culMapper.Match("test", "thereligion", 4, 49, "ROM"));
	}

	[Fact]
	public void CultureFailsWithWrongHistoricalTag() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4 historicalTag = ROM }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Null(culMapper.Match("test", "thereligion", 4, 49, "WRO"));
	}

	[Fact]
	public void CultureMatchesWithNoHistoricalTag() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4 }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Equal("culture", culMapper.Match("test", "thereligion", 4, 49, ""));
	}

	[Fact]
	public void CultureMatchesWithNoHistoricalTagInRule() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4}"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Equal("culture", culMapper.Match("test", "thereligion", 4, 49, "ROM"));
	}

	[Fact]
	public void CultureFailsWithHistoricalTagInRule() {
		var reader = new BufferedReader("""
		link = {
			ck3=culture
			imp=qwe imp=test imp=poi
			religion=thereligion
			ck3Province=4
			historicalTag=ROM
		}
		""");
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Null(culMapper.Match("test", "thereligion", 4, 49, ""));
	}

	[Fact]
	public void NonMatchGivesEmptyOptionalWithNonReligiousMatch() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = culture }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Null(culMapper.NonReligiousMatch("nonMatchingCulture", "", 56, 49, "ROM"));
	}

	[Fact]
	public void SimpleCultureMatchesWithNonReligiousMatch() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = test }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Equal("culture", culMapper.NonReligiousMatch("test", "", 56, 49, "ROM"));
	}

	[Fact]
	public void SimpleCultureCorrectlyMatchesWithNonReligiousMatch() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Equal("culture", culMapper.NonReligiousMatch("test", "", 56, 49, "ROM"));
	}

	[Fact]
	public void CultureFailsWithCorrectReligionWithNonReligiousMatch() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Null(culMapper.NonReligiousMatch("test", "thereligion", 56, 49, "ROM"));
	}

	[Fact]
	public void CultureFailsWithWrongReligionWithNonReligiousMatch() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Null(culMapper.NonReligiousMatch("test", "unreligion", 56, 49, "ROM"));
	}

	[Fact]
	public void CultureFailsWithNoReligionWithNonReligiousMatch() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Null(culMapper.NonReligiousMatch("test", "", 56, 49, "ROM"));
	}

	[Fact]
	public void CultureMatchesWithReligionAndNonReligiousLinkWithNonReligiousMatch() {
		var reader = new BufferedReader(
			"link = { ck3 = culture imp = qwe imp = test imp = poi }"
		);
		var culMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Equal("culture", culMapper.NonReligiousMatch("test", "thereligion", 56, 49, "ROM"));
	}

	[Fact]
	public void VariablesWorkInLinks() {
		var reader = new BufferedReader(
			"@germ_cultures = \"imp=sennonian imp=bellovacian imp=veliocassian imp=morinian\" \r\n" +
			"link = { ck3=low_germ @germ_cultures impProvince=1}\r\n" +
			"link = { ck3=high_germ @germ_cultures impProvince=2}"
		);
		var cultureMapper = new CultureMapper(reader, irRegionMapper, new CK3RegionMapper());

		Assert.Null(cultureMapper.NonReligiousMatch("missing_culture", "", 0, irProvinceId: 1, ""));
		Assert.Equal("low_germ", cultureMapper.NonReligiousMatch("bellovacian", "", 0, irProvinceId: 1, ""));
		Assert.Equal("high_germ", cultureMapper.NonReligiousMatch("bellovacian", "", 0, irProvinceId: 2, ""));
	}
}