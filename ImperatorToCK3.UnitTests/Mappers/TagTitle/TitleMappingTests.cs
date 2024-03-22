using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using FluentAssertions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.TagTitle;
using System;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.TagTitle;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class TitleMappingTests {
	[Fact]
	public void SimpleTagMatch() {
		var reader = new BufferedReader("{ ck3 = e_roman_empire ir = ROM }");
		var mapping = TitleMapping.Parse(reader);
		var match = mapping.RankMatch("ROM", TitleRank.empire, maxTitleRank: TitleRank.empire);

		Assert.Equal("e_roman_empire", match);
	}

	[Fact]
	public void SimpleTagMatchFailsOnWrongTag() {
		var reader = new BufferedReader("{ ck3 = e_roman_empire ir = REM }");
		var mapping = TitleMapping.Parse(reader);
		var match = mapping.RankMatch("ROM", TitleRank.empire, maxTitleRank: TitleRank.empire);

		Assert.Null(match);
	}

	[Fact]
	public void SimpleTagMatchFailsOnNoTag() {
		var reader = new BufferedReader("{ ck3 = e_roman_empire }");
		var mapping = TitleMapping.Parse(reader);
		var match = mapping.RankMatch("ROM", TitleRank.empire, maxTitleRank: TitleRank.empire);

		Assert.Null(match);
	}

	[Fact]
	public void TagRankMatch() {
		var reader = new BufferedReader("{ ck3 = e_roman_empire ir = ROM rank = e }");
		var mapping = TitleMapping.Parse(reader);
		var match = mapping.RankMatch("ROM", TitleRank.empire, maxTitleRank: TitleRank.empire);

		Assert.Equal("e_roman_empire", match);
	}

	[Fact]
	public void TagRankMatchFailsOnWrongRank() {
		var reader = new BufferedReader("{ ck3 = e_roman_empire ir = ROM rank = k }");
		var mapping = TitleMapping.Parse(reader);
		var match = mapping.RankMatch("ROM", TitleRank.empire, maxTitleRank: TitleRank.empire);

		Assert.Null(match);
	}

	[Fact]
	public void TagRankMatchSucceedsOnNoRank() {
		var reader = new BufferedReader("{ ck3 = e_roman_empire ir = ROM }");
		var mapping = TitleMapping.Parse(reader);
		var match = mapping.RankMatch("ROM", TitleRank.empire, maxTitleRank: TitleRank.empire);

		Assert.Equal("e_roman_empire", match);
	}

	[Fact]
	public void GovernorshipToDeJureDuchyMappingFailsIfDuchyIsNot60PercentControlled() {
		var irProvinces = new ProvinceCollection {
			new(1), new(2), new(3)
		};

		var irCharacters = new CharacterCollection();
		const ulong irGovernorId = 25212;
		var governor = new ImperatorToCK3.Imperator.Characters.Character(irGovernorId);
		irCharacters.Add(governor);

		var irCountries = new CountryCollection();
		const ulong irCountryId = 589;
		var countryReader = new BufferedReader("tag=PRY capital=1");
		var country = Country.Parse(countryReader, irCountryId);
		irCountries.Add(country);
		
		var irModFS = new ModFilesystem("TestFiles/Imperator/game", Array.Empty<Mod>());
		var irAreas = new AreaCollection();
		irAreas.LoadAreas(irModFS, irProvinces);
		var irMapData = new MapData(irModFS);
		var irRegionMapper = new ImperatorRegionMapper(irAreas, irMapData);
		irRegionMapper.LoadRegions(irModFS, new ColorFactory());
		const string irRegionId = "galatia_region";
		Assert.True(irRegionMapper.RegionNameIsValid(irRegionId));

		var provinceMapper = new ProvinceMapper();
		const string provinceMappingsPath = "TestFiles/LandedTitlesTests/province_mappings.txt";
		provinceMapper.LoadMappings(provinceMappingsPath, "test_version");
		Assert.Equal((ulong)1, provinceMapper.GetCK3ProvinceNumbers(1).First());
		Assert.Equal((ulong)2, provinceMapper.GetCK3ProvinceNumbers(2).First());
		Assert.Equal((ulong)3, provinceMapper.GetCK3ProvinceNumbers(3).First());
		
		var governorshipReader = new BufferedReader(
			$"who={irCountryId} " +
			$"character={irGovernorId} " +
			"start_date=450.10.1 " +
			$"governorship = \"{irRegionId}\""
		);
		var irGovernorship = new Governorship(governorshipReader, irCountries, irRegionMapper);
		Assert.Equal(irRegionId, irGovernorship.Region.Id);
		Assert.Equal(irCountryId, irGovernorship.Country.Id);
		var jobs = new JobsDB();
		jobs.Governorships.Add(irGovernorship);
		
		const string duchyId = "d_galatia";
		var titles = new Title.LandedTitles();
		var titlesReader = new BufferedReader($$"""
			{{duchyId}} = { 
				c_county1 = { b_barony1 = { province = 1 } }
				c_county2 = { b_barony2 = { province = 2 } }
				c_county3 = { b_barony3 = { province = 3 } }
			}
		""");
		titles.LoadTitles(titlesReader);
		Assert.Contains(duchyId, titles.GetDeJureDuchies().Select(d => d.Id));
		
		var mappingReader = new BufferedReader($"{{ ck3={duchyId} ir={irRegionId} rank=d }}");
		var mapping = TitleMapping.Parse(mappingReader);

		// Governorship holds 0/3 provinces in the duchy, so it should not be mapped to the duchy.
		irGovernorship.GetIRProvinces(irProvinces).Should().BeEmpty();
		irGovernorship.GetCK3ProvinceIds(irProvinces, provinceMapper).Should().BeEmpty();
		var match = mapping.GovernorshipMatch(TitleRank.duchy, titles, irGovernorship, provinceMapper, irProvinces);
		Assert.Null(match);
		
		var irProvince1 = irProvinces[1];
		irProvince1.OwnerCountry = country;
		irGovernorship.GetIRProvinces(irProvinces).Should().Equal(irProvinces[1]);
		irGovernorship.GetCK3ProvinceIds(irProvinces, provinceMapper).Should().Equal(1);
		// Governorship holds 1/3 provinces in the duchy, so it should not be mapped to the duchy.
		match = mapping.GovernorshipMatch(TitleRank.duchy, titles, irGovernorship, provinceMapper, irProvinces);
		Assert.Null(match);
		
		var irProvince2 = irProvinces[2];
		irProvince2.OwnerCountry = country;
		irGovernorship.GetIRProvinces(irProvinces).Should().Equal(irProvinces[1], irProvinces[2]);
		irGovernorship.GetCK3ProvinceIds(irProvinces, provinceMapper).Should().Equal(1, 2);
		// Governorship holds 2/3 provinces in the duchy, so it should be mapped to the duchy.
		match = mapping.GovernorshipMatch(TitleRank.duchy, titles, irGovernorship, provinceMapper, irProvinces);
		Assert.Equal(duchyId, match);
		
		var irProvince3 = irProvinces[3];
		irProvince3.OwnerCountry = country;
		irGovernorship.GetIRProvinces(irProvinces).Should().Equal(irProvinces[1], irProvinces[2], irProvinces[3]);
		irGovernorship.GetCK3ProvinceIds(irProvinces, provinceMapper).Should().Equal(1, 2, 3);
		// Governorship holds 3/3 provinces in the duchy, so it should be mapped to the duchy.
		match = mapping.GovernorshipMatch(TitleRank.duchy, titles, irGovernorship, provinceMapper, irProvinces);
		Assert.Equal(duchyId, match);
	}
}