using commonItems;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Diplomacy;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.TagTitle;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class TagTitleMapperTests {
	private const string ImperatorRoot = "TestFiles/Imperator/root";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly AreaCollection Areas = new();
	private readonly ImperatorRegionMapper irRegionMapper = new(Areas);
	private const string tagTitleMappingsPath = "TestFiles/configurables/title_map.txt";
	private const string governorshipTitleMappingsPath = "TestFiles/configurables/governorMappings.txt";
	private const string rankMappingsPath = "TestFiles/configurables/country_rank_map.txt";
	private static readonly CultureCollection cultures;
	private static readonly ColorFactory ColorFactory = new();
	
	static TagTitleMapperTests() {
		var ck3ModFlags = new List<string>();
		var pillars = new PillarCollection(ColorFactory, ck3ModFlags);
		cultures = new CultureCollection(ColorFactory, pillars, ck3ModFlags);
	}
	
	public TagTitleMapperTests() {
		irRegionMapper.LoadRegions(irModFS, ColorFactory);
	}

	[Fact]
	public void TitleCanBeMatchedFromTag() {
		var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath, rankMappingsPath); // reads title_map.txt from TestFiles
		var country = Country.Parse(new BufferedReader("tag=CRT"), 1);
		for (ulong i = 1; i < 200; ++i) { // makes the country a major power
			var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
			country.RegisterProvince(province);
		}
		var match = mapper.GetTitleForTag(country);

		Assert.Equal("k_krete", match);
	}
	[Fact]
	public void TitleCanBeMatchedFromGovernorship() {
		var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath, rankMappingsPath); // reads title_map.txt from TestFiles
		const ulong romeId = 1;
		mapper.RegisterCountry(romeId, "e_roman_empire");

		var irCountries = new CountryCollection();
		irCountries.LoadCountries(new BufferedReader($"{romeId} = {{ tag=ROM }}"));
		var titles = new Title.LandedTitles();
		var ck3Religions = new ReligionCollection(titles);
		var ck3RegionMapper = new CK3RegionMapper();
		var provMapper = new ProvinceMapper();
		titles.ImportImperatorCountries(irCountries,
			Array.Empty<Dependency>(),
			mapper,
			new LocDB("english"),
			provMapper,
			new CoaMapper(),
			new GovernmentMapper(ck3GovernmentIds: Array.Empty<string>()),
			new SuccessionLawMapper(),
			new DefiniteFormMapper(),
			new ReligionMapper(ck3Religions, irRegionMapper, ck3RegionMapper),
			new CultureMapper(irRegionMapper, ck3RegionMapper, cultures),
			new NicknameMapper(),
			new CharacterCollection(),
			new Date(),
			new Configuration(),
			new List<KeyValuePair<Country, Dependency?>>()
		);
		
		irRegionMapper.Regions.Add(new ImperatorRegion("central_italy_region", new BufferedReader(), Areas, ColorFactory));

		var governorshipReader = new BufferedReader("who=1 governorship=central_italy_region");
		var centralItalyGov = new Governorship(governorshipReader, irCountries, irRegionMapper);
		var irProvinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection();
		var provinces = new ProvinceCollection();
		var match = mapper.GetTitleForGovernorship(centralItalyGov, titles, irProvinces, provinces, irRegionMapper, provMapper);

		Assert.Equal("k_romagna", match);
	}

	[Fact]
	public void TitleCanBeMatchedByRanklessLink() {
		var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath, rankMappingsPath); // reads title_map.txt from TestFiles
		var country = Country.Parse(new BufferedReader("tag=RAN"), 1);
		for (ulong i = 1; i < 200; ++i) { // makes the country a major power
			var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
			country.RegisterProvince(province);
		}
		var match = mapper.GetTitleForTag(country);

		Assert.Equal("d_rankless", match);
	}

	[Fact]
	public void TitleCanBeGeneratedFromTag() {
		var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath, rankMappingsPath);
		var rom = Country.Parse(new BufferedReader("tag=ROM"), 1);
		for (ulong i = 0; i < 20; ++i) { // makes the country a local power
			rom.RegisterProvince(new ImperatorToCK3.Imperator.Provinces.Province(i));
		}
		var match = mapper.GetTitleForTag(rom, "Rome", maxTitleRank: TitleRank.empire);

		var dre = Country.Parse(new BufferedReader("tag=DRE"), 2);
		for (ulong i = 0; i < 20; ++i) { // makes the country a local power
			dre.RegisterProvince(new ImperatorToCK3.Imperator.Provinces.Province(i));
		}
		var match2 = mapper.GetTitleForTag(dre, "Dre Empire", maxTitleRank: TitleRank.empire);

		Assert.Equal("k_IRTOCK3_ROM", match);
		Assert.Equal("e_IRTOCK3_DRE", match2);
	}
	[Fact]
	public void TitleCanBeGeneratedFromGovernorship() {
		var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath, rankMappingsPath);
		const ulong romeId = 1;
		const ulong dreId = 2;
		mapper.RegisterCountry(romeId, "e_roman_empire");
		mapper.RegisterCountry(dreId, "k_dre_empire");

		var impCountries = new CountryCollection();
		impCountries.LoadCountries(new BufferedReader($"{romeId}={{tag=ROM}} {dreId}={{tag=DRE}}"));
		var titles = new Title.LandedTitles();
		var ck3Religions = new ReligionCollection(titles);
		var ck3RegionMapper = new CK3RegionMapper();
		var provMapper = new ProvinceMapper();
		titles.ImportImperatorCountries(impCountries,
			Array.Empty<Dependency>(),
			mapper,
			new LocDB("english"),
			provMapper,
			new CoaMapper(),
			new GovernmentMapper(ck3GovernmentIds: Array.Empty<string>()),
			new SuccessionLawMapper(),
			new DefiniteFormMapper(),
			new ReligionMapper(ck3Religions, irRegionMapper, ck3RegionMapper),
			new CultureMapper(irRegionMapper, ck3RegionMapper, cultures),
			new NicknameMapper(),
			new CharacterCollection(),
			new Date(),
			new Configuration(),
			new List<KeyValuePair<Country, Dependency?>>()
		);

		irRegionMapper.Regions.Add(new ImperatorRegion("apulia_region", new BufferedReader(), Areas, ColorFactory));
		irRegionMapper.Regions.Add(new ImperatorRegion("pepe_region", new BufferedReader(), Areas, ColorFactory));
		
		var apuliaGovReader = new BufferedReader($"who={romeId} governorship=apulia_region");
		var apuliaGov = new Governorship(apuliaGovReader, impCountries, irRegionMapper);
		var pepeGovReader = new BufferedReader($"who={dreId} governorship=pepe_region");
		var pepeGov = new Governorship(pepeGovReader, impCountries, irRegionMapper);

		var irProvinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection();
		var provinces = new ProvinceCollection();
		var match = mapper.GetTitleForGovernorship(apuliaGov, titles, irProvinces, provinces, irRegionMapper, provMapper);
		var match2 = mapper.GetTitleForGovernorship(pepeGov, titles, irProvinces, provinces, irRegionMapper, provMapper);

		Assert.Equal("k_IRTOCK3_ROM_apulia_region", match);
		Assert.Equal("d_IRTOCK3_DRE_pepe_region", match2);
	}

	[Fact]
	public void GetTitleForTagReturnsNullOnEmptyTag() {
		var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath, rankMappingsPath);
		var country = Country.Parse(new BufferedReader(string.Empty), 1);
		Assert.Empty(country.Tag);
		var match = mapper.GetTitleForTag(country, "", maxTitleRank: TitleRank.empire);

		Assert.Null(match);
	}
	[Fact]
	public void GetTitleGovernorshipTagReturnsNullOnCountryWithNoCK3Title() {
		var output = new StringWriter();
		Console.SetOut(output);
		
		irRegionMapper.Regions.Add(new ImperatorRegion("apulia_region", new BufferedReader(), Areas, ColorFactory));

		var irProvinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection();
		var provMapper = new ProvinceMapper();

		var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath, rankMappingsPath);
		var country = new Country(1);
		var countries = new CountryCollection {country};
		var apuliaGovReader = new BufferedReader("who=1 governorship=apulia_region");
		var apuliaGov = new Governorship(apuliaGovReader, countries, irRegionMapper);
		var match = mapper.GetTitleForGovernorship(apuliaGov, new Title.LandedTitles(), irProvinces, new ProvinceCollection(), irRegionMapper, provMapper);

		Assert.Null(match);
		Assert.Contains("[WARN] Country  has no associated CK3 title!", output.ToString());
	}

	[Fact]
	public void TagCanBeRegistered() {
		var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath, rankMappingsPath);
		const ulong borId = 1;
		mapper.RegisterCountry(borId, "e_boredom");
		var country = Country.Parse(new BufferedReader("tag=BOR"), borId);
		for (ulong i = 1; i < 20; ++i) { // makes the country a local power
			var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
			country.RegisterProvince(province);
		}
		var match = mapper.GetTitleForTag(country);

		Assert.Equal("e_boredom", match);
	}
	[Fact]
	public void GovernorshipCanBeRegistered() {
		var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath, rankMappingsPath);
		const ulong borId = 1;
		mapper.RegisterCountry(borId, "e_roman_empire");

		var irProvinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection();
		var impCountries = new CountryCollection();
		impCountries.LoadCountries(new BufferedReader($"{borId}={{tag=BOR}}"));
		var titles = new Title.LandedTitles();

		var ck3Religions = new ReligionCollection(titles);
		var ck3RegionMapper = new CK3RegionMapper();
		var provMapper = new ProvinceMapper();
		titles.ImportImperatorCountries(impCountries,
			Array.Empty<Dependency>(),
			mapper,
			new LocDB("english"),
			provMapper,
			new CoaMapper(),
			new GovernmentMapper(ck3GovernmentIds: Array.Empty<string>()),
			new SuccessionLawMapper(),
			new DefiniteFormMapper(),
			new ReligionMapper(ck3Religions, irRegionMapper, ck3RegionMapper),
			new CultureMapper(irRegionMapper, ck3RegionMapper, cultures),
			new NicknameMapper(),
			new CharacterCollection(),
			new Date(),
			new Configuration(),
			new List<KeyValuePair<Country, Dependency?>>()
		);

		var ck3Provinces = new ProvinceCollection();
		
		irRegionMapper.Regions.Add(new ImperatorRegion("aquitaine_region", new BufferedReader(), Areas, ColorFactory));
		mapper.RegisterGovernorship("aquitaine_region", "BOR", "k_atlantis");

		var aquitaneGovReader = new BufferedReader("who=1 governorship=aquitaine_region");
		var aquitaneGov = new Governorship(aquitaneGovReader, impCountries, irRegionMapper);
		var match = mapper.GetTitleForGovernorship(aquitaneGov, titles, irProvinces, ck3Provinces, irRegionMapper, provMapper);

		Assert.Equal("k_atlantis", match);
	}

	[Fact]
	public void GetCK3TitleRankReturnsCorrectRank() {
		var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath, rankMappingsPath);
		var tag1 = Country.Parse(new BufferedReader("tag=TEST_TAG1"), 1);
		for (ulong i = 1; i < 20; ++i) { // makes the country a local power
			var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
			tag1.RegisterProvince(province);
		}
		// Should be an empire because of the name containing "Empire".
		Assert.Equal('e', mapper.GetTitleForTag(tag1, "Test Empire", maxTitleRank: TitleRank.empire)![0]);

		var tag2 = Country.Parse(new BufferedReader("tag=TEST_TAG2"), 2);
		for (ulong i = 1; i < 2; ++i) { // makes the country a city state
			var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
			tag2.RegisterProvince(province);
		}
		// Should be a kingdom because of the name containing "Kingdom".
		Assert.Equal('k', mapper.GetTitleForTag(tag2, "Test Kingdom", maxTitleRank: TitleRank.empire)![0]);

		var tag3 = Country.Parse(new BufferedReader("tag=TEST_TAG3"), 3); // migrant horde
		Assert.Equal('d', mapper.GetTitleForTag(tag3)![0]);

		var tag4 = Country.Parse(new BufferedReader("tag=TEST_TAG4"), 4);
		for (ulong i = 1; i < 2; ++i) { // makes the country a city state
			var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
			tag4.RegisterProvince(province);
		}
		Assert.Equal('d', mapper.GetTitleForTag(tag4)![0]);

		var tag5 = Country.Parse(new BufferedReader("tag=TEST_TAG5"), 5);
		for (ulong i = 1; i < 20; ++i) { // makes the country a local power
			var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
			tag5.RegisterProvince(province);
		}
		Assert.Equal('k', mapper.GetTitleForTag(tag5)![0]);

		var tag6 = Country.Parse(new BufferedReader("tag=TEST_TAG6"), 6);
		for (ulong i = 1; i < 40; ++i) { // makes the country a regional power
			var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
			tag6.RegisterProvince(province);
		}
		Assert.Equal('k', mapper.GetTitleForTag(tag6)![0]);

		var tag7 = Country.Parse(new BufferedReader("tag=TEST_TAG7"), 7);
		for (ulong i = 1; i < 200; ++i) { // makes the country a major power
			var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
			tag7.RegisterProvince(province);
		}
		Assert.Equal('k', mapper.GetTitleForTag(tag7)![0]);

		var tag8 = Country.Parse(new BufferedReader("tag=TEST_TAG8"), 8);
		for (ulong i = 1; i < 501; ++i) { // makes the country a great power
			var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
			tag8.RegisterProvince(province);
		}
		Assert.Equal('e', mapper.GetTitleForTag(tag8, "Testonia", maxTitleRank: TitleRank.empire)![0]);
		
		// Rank can be overridden by name containing "Duchy", "Principality" or "Dukedom".
		var tag9 = Country.Parse(new BufferedReader("tag=TEST_TAG9"), 9);
		for (ulong i = 1; i < 501; ++i) { // makes the country a great power
			tag9.RegisterProvince(new(i));
		}
		Assert.Equal('d', mapper.GetTitleForTag(tag9, "Test Duchy", maxTitleRank: TitleRank.empire)![0]);
		
		var tag10 = Country.Parse(new BufferedReader("tag=TEST_TAG10"), 10);
		for (ulong i = 1; i < 501; ++i) { // makes the country a great power
			tag10.RegisterProvince(new(i));
		}
		Assert.Equal('d', mapper.GetTitleForTag(tag10, "Test Principality", maxTitleRank: TitleRank.empire)![0]);
		
		var tag11 = Country.Parse(new BufferedReader("tag=TEST_TAG11"), 11);
		for (ulong i = 1; i < 501; ++i) { // makes the country a great power
			tag11.RegisterProvince(new(i));
		}
		Assert.Equal('d', mapper.GetTitleForTag(tag11, "Test Dukedom", maxTitleRank: TitleRank.empire)![0]);
	}
}