﻿using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Religion;
using Xunit;
using System;

namespace ImperatorToCK3.UnitTests.Mappers.Religion;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ReligionMapperTests {
	private const string ImperatorRoot = "TestFiles/Imperator/root";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly AreaCollection areas = new();
	private static readonly ImperatorRegionMapper irRegionMapper = new(irModFS, areas);

	private const string CK3Root = "TestFiles/CK3/game";
	private readonly ModFilesystem ck3ModFs = new(CK3Root, new Mod[] { });

	[Fact]
	public void NonMatchGivesNull() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		var ck3RegionMapper = new CK3RegionMapper();

		var reader = new BufferedReader("link = { ck3 = ck3Faith imp = impReligion }");
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, ck3RegionMapper);

		var ck3FaithId = mapper.Match("nonMatchingReligion", null, null, null, new Configuration());
		Assert.Null(ck3FaithId);
	}

	[Fact]
	public void CK3FaithCanBeFound() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		var ck3RegionMapper = new CK3RegionMapper();

		var reader = new BufferedReader("link = { ck3 = ck3Faith imp = impReligion }");
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, ck3RegionMapper);

		var ck3FaithId = mapper.Match("impReligion", 45, 456, null, new Configuration());
		Assert.Equal("ck3Faith", ck3FaithId);
	}

	[Fact]
	public void MultipleImperatorReligionsCanBeInARule() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		var ck3RegionMapper = new CK3RegionMapper();

		var reader = new BufferedReader(
			"link = { ck3 = ck3Faith imp = impReligion imp = impReligion2 }"
		);
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, ck3RegionMapper);

		var ck3FaithId = mapper.Match("impReligion2", 45, 456, null, new Configuration());
		Assert.Equal("ck3Faith", ck3FaithId);
	}

	[Fact]
	public void CorrectRuleMatches() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		var ck3RegionMapper = new CK3RegionMapper();

		var reader = new BufferedReader(
			"link = { ck3 = ck3Faith imp = impReligion }" +
			"link = { ck3 = ck3Faith2 imp = impReligion2 }"
		);
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, ck3RegionMapper);

		var ck3FaithId = mapper.Match("impReligion2", 45, 456, null, new Configuration());
		Assert.Equal("ck3Faith2", ck3FaithId);
	}

	[Fact]
	public void MappingCanBeMatchedByHistoricalTag() {
		var ck3Religions = new ReligionCollection(new Title.LandedTitles());
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		var ck3RegionMapper = new CK3RegionMapper();

		const string irReligion = "impReligion";
		var reader = new BufferedReader($$"""
			link = { ck3 = ck3Faith imp = {{irReligion}} historicalTag = ROM }
			link = { ck3 = ck3Faith2 imp = {{irReligion}} historicalTag = ARM }
			link = { ck3 = ck3Faith3 imp = {{irReligion}} }
		""");
		var mapper = new ReligionMapper(reader, ck3Religions, irRegionMapper, ck3RegionMapper);

		Assert.Equal("ck3Faith", mapper.Match(irReligion, 45, 456, "ROM", new Configuration()));
		Assert.Equal("ck3Faith2", mapper.Match(irReligion, 45, 456, "ARM", new Configuration()));
		Assert.Equal("ck3Faith3", mapper.Match(irReligion, 45, 456, "LOL", new Configuration()));
	}
}