using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Mappers.Religion;
using Xunit;
using System;

namespace ImperatorToCK3.UnitTests.Mappers.Religion;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ReligionMapperTests {
	private const string ImperatorRoot = "TestFiles/Imperator/root";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly MapData irMapData = new(irModFS);
	private static readonly AreaCollection areas = new();
	private static readonly ImperatorRegionMapper irRegionMapper = new(areas, irMapData);

	private const string CK3Root = "TestFiles/CK3/game";
	private readonly ModFilesystem ck3ModFs = new(CK3Root, Array.Empty<Mod>());
	
	public ReligionMapperTests() {
		irRegionMapper.LoadRegions(irModFS, new ColorFactory());
	}

	[Fact]
	public void NonMatchGivesNull() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		var ck3RegionMapper = new CK3RegionMapper();

		var reader = new BufferedReader("link = { ck3 = ck3Faith ir = impReligion }");
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, ck3RegionMapper);

		var ck3FaithId = mapper.Match("nonMatchingReligion", null, null, null, null, new Configuration());
		Assert.Null(ck3FaithId);
	}

	[Fact]
	public void CK3FaithCanBeFound() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		var ck3RegionMapper = new CK3RegionMapper();

		var reader = new BufferedReader("link = { ck3 = ck3Faith ir = impReligion }");
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, ck3RegionMapper);

		var religionMatch = mapper.Match("impReligion", null, 45, 456, null, new Configuration());
		Assert.Equal("ck3Faith", religionMatch?.FaithId);
	}

	[Fact]
	public void MultipleImperatorReligionsCanBeInARule() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		var ck3RegionMapper = new CK3RegionMapper();

		var reader = new BufferedReader(
			"link = { ck3 = ck3Faith ir = impReligion ir = impReligion2 }"
		);
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, ck3RegionMapper);

		var religionMatch = mapper.Match("impReligion2", null, 45, 456, null, new Configuration());
		Assert.Equal("ck3Faith", religionMatch?.FaithId);
	}

	[Fact]
	public void CorrectRuleMatches() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		var ck3RegionMapper = new CK3RegionMapper();

		var reader = new BufferedReader(
			"link = { ck3 = ck3Faith ir = impReligion }" +
			"link = { ck3 = ck3Faith2 ir = impReligion2 }"
		);
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, ck3RegionMapper);

		var religionMatch = mapper.Match("impReligion2", null, 45, 456, null, new Configuration());
		Assert.Equal("ck3Faith2", religionMatch?.FaithId);
	}

	[Fact]
	public void MappingCanBeMatchedByHistoricalTag() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		var ck3RegionMapper = new CK3RegionMapper();

		const string irReligion = "impReligion";
		var reader = new BufferedReader($$"""
			link = { ck3 = ck3Faith ir = {{irReligion}} historicalTag = ROM }
			link = { ck3 = ck3Faith2 ir = {{irReligion}} historicalTag = ARM }
			link = { ck3 = ck3Faith3 ir = {{irReligion}} }
		""");
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, ck3RegionMapper);

		Assert.Equal("ck3Faith", mapper.Match(irReligion, null, 45, 456, "ROM", new Configuration())?.FaithId);
		Assert.Equal("ck3Faith2", mapper.Match(irReligion, null, 45, 456, "ARM", new Configuration())?.FaithId);
		Assert.Equal("ck3Faith3", mapper.Match(irReligion, null, 45, 456, "LOL", new Configuration())?.FaithId);
	}

	[Fact]
	public void RiteIdIsPreferredOverFaithId() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		var rite = new Rite(
			"test_rite",
			new BufferedReader("{ faith = religion_a_faith }"),
			ck3Religions,
			new ColorFactory());
		ck3Religions.Rites.AddOrReplace(rite);

		var reader = new BufferedReader("link = { ck3 = test_rite ir = impReligion }");
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, new CK3RegionMapper());

		var match = mapper.Match("impReligion", null, null, null, null, new Configuration());
		Assert.Equal("religion_a_faith", match?.FaithId);
		Assert.Equal("test_rite", match?.RiteId);
	}

	[Fact]
	public void FaithIdCanBeMappedWithoutRite() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());

		var reader = new BufferedReader("link = { ck3 = religion_a_faith ir = impReligion }");
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, new CK3RegionMapper());

		var match = mapper.Match("impReligion", null, null, null, null, new Configuration());
		Assert.Equal("religion_a_faith", match?.FaithId);
		Assert.Null(match?.RiteId);
	}

	[Fact]
	public void UnknownCk3IdIsDropped() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());

		var reader = new BufferedReader("link = { ck3 = nonexistent_rite_or_faith ir = impReligion }");
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, new CK3RegionMapper());

		Assert.Null(mapper.Match("impReligion", null, null, null, null, new Configuration()));
	}

	[Fact]
	public void UnresolvedMappingFallsThroughToNextValidMapping() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());

		var reader = new BufferedReader("""
			link = { ck3 = nonexistent_rite_or_faith ir = impReligion }
			link = { ck3 = ck3Faith ir = impReligion }
			""");
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, new CK3RegionMapper());

		var match = mapper.Match("impReligion", null, null, null, null, new Configuration());
		Assert.Equal("ck3Faith", match?.FaithId);
		Assert.Null(match?.RiteId);
	}

	[Fact]
	public void RiteMappingTakesPriorityAndFaithMappingServesAsFallback() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		var rite = new Rite(
			"priority_test_rite",
			new BufferedReader("{ faith = religion_a_faith }"),
			ck3Religions,
			new ColorFactory());
		ck3Religions.Rites.AddOrReplace(rite);

		var reader = new BufferedReader("""
			link = { ck3 = priority_test_rite ir = impReligion historicalTag = ROM }
			link = { ck3 = religion_a_faith ir = impReligion }
			""");
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, new CK3RegionMapper());

		var priorityMatch = mapper.Match("impReligion", null, null, null, "ROM", new Configuration());
		Assert.Equal("religion_a_faith", priorityMatch?.FaithId);
		Assert.Equal("priority_test_rite", priorityMatch?.RiteId);

		var fallbackMatch = mapper.Match("impReligion", null, null, null, "LOL", new Configuration());
		Assert.Equal("religion_a_faith", fallbackMatch?.FaithId);
		Assert.Null(fallbackMatch?.RiteId);
	}

	[Fact]
	public void FaithlessRiteMappingIsDropped() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		var rite = new Rite(
			"faithless_test_rite",
			new BufferedReader("{ color = rgb { 1 2 3 } }"),
			ck3Religions,
			new ColorFactory());
		ck3Religions.Rites.AddOrReplace(rite);

		var reader = new BufferedReader("link = { ck3 = faithless_test_rite ir = impReligion }");
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, new CK3RegionMapper());

		Assert.Null(mapper.Match("impReligion", null, null, null, null, new Configuration()));
	}
}