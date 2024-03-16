using commonItems;
using commonItems.Colors;
using commonItems.Mods;
using FluentAssertions;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Diplomacy;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Imperator.States;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Mappers.Trait;
using ImperatorToCK3.UnitTests.TestHelpers;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System;

namespace ImperatorToCK3.UnitTests.CK3.Characters;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CharacterCollectionTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly MapData irMapData = new(irModFS);
	private static readonly ImperatorRegionMapper irRegionMapper;
	private readonly string provinceMappingsPath = "TestFiles/LandedTitlesTests/province_mappings.txt";
	private readonly ModFilesystem ck3ModFS = new("TestFiles/LandedTitlesTests/CK3/game", new List<Mod>());
	private static readonly CultureCollection cultures;

	static CharacterCollectionTests() {
		var colorFactory = new ColorFactory();
		
		var states = new StateCollection();
		var countries = new CountryCollection();
		ImperatorToCK3.Imperator.Provinces.ProvinceCollection irProvinces = new();
		irProvinces.LoadProvinces(
			new BufferedReader(
				"1={} 2={} 3={} 4={} 5={} 6={} 7={} 8={} 9={} 69={}"
			),
			states,
			countries
		);
		AreaCollection areas = new();
		areas.LoadAreas(irModFS, irProvinces);
		irRegionMapper = new ImperatorRegionMapper(areas, irMapData);
		irRegionMapper.LoadRegions(irModFS, colorFactory);
		
		var ck3ModFlags = new List<string>();
		cultures = new CultureCollection(colorFactory, new PillarCollection(colorFactory, ck3ModFlags), ck3ModFlags);
	}

	[Fact]
	public void MarriageDateCanBeEstimatedFromChild() {
		var endDate = new Date(1100, 1, 1, AUC: true);
		var configuration = new Configuration { CK3BookmarkDate = endDate };
		var imperatorWorld = new TestImperatorWorld(configuration);

		var male = new ImperatorToCK3.Imperator.Characters.Character(1);
		var female = new ImperatorToCK3.Imperator.Characters.Character(2);
		var childReader = new BufferedReader("father=1 mother=2 birth_date=900.1.1");
		var child = ImperatorToCK3.Imperator.Characters.Character.Parse(childReader, "3", null);

		male.Spouses.Add(1, female);
		male.Children.Add(3, child);
		female.Children.Add(3, child);
		imperatorWorld.Characters.Add(male);
		imperatorWorld.Characters.Add(female);
		imperatorWorld.Characters.Add(child);

		var landedTitles = new Title.LandedTitles();
		var ck3Religions = new ReligionCollection(landedTitles);
		var ck3RegionMapper = new CK3RegionMapper();
		var ck3Characters = new CharacterCollection();
		ck3Characters.ImportImperatorCharacters(
			imperatorWorld,
			new ReligionMapper(ck3Religions, irRegionMapper, ck3RegionMapper),
			new CultureMapper(irRegionMapper, ck3RegionMapper, cultures),
			cultures,
			new TraitMapper(),
			new NicknameMapper(),
			new ProvinceMapper(),
			new DeathReasonMapper(),
			new DNAFactory(irModFS, ck3ModFS),
			endDate,
			configuration);

		Assert.Collection(ck3Characters.OrderBy(c => c.Id),
			ck3Male => {
				var marriageDate = ck3Male.History.Fields["spouses"].DateToEntriesDict.FirstOrDefault().Key;
				Assert.Equal(new Date(899, 3, 27, AUC: true), marriageDate);
			},
			ck3Female => {
				Assert.Equal("imperator2", ck3Female.Id);
			},
			ck3Child => {
				Assert.Equal("imperator3", ck3Child.Id);
			});
	}

	[Fact]
	public void MarriageDateCanBeEstimatedFromUnbornChild() {
		var endDate = new Date(1100, 1, 1, AUC: true);
		var configuration = new Configuration { CK3BookmarkDate = endDate };
		var imperatorWorld = new TestImperatorWorld(configuration);

		var male = new ImperatorToCK3.Imperator.Characters.Character(1);
		var femaleReader = new BufferedReader("unborn={ { mother=2 father=1 date=900.1.1 } }");
		var female = ImperatorToCK3.Imperator.Characters.Character.Parse(femaleReader, "2", null);

		male.Spouses.Add(1, female);
		imperatorWorld.Characters.Add(male);
		imperatorWorld.Characters.Add(female);

		var landedTitles = new Title.LandedTitles();
		var ck3Religions = new ReligionCollection(landedTitles);
		var ck3RegionMapper = new CK3RegionMapper();
		var ck3Characters = new CharacterCollection();
		ck3Characters.ImportImperatorCharacters(
			imperatorWorld,
			new ReligionMapper(ck3Religions, irRegionMapper, ck3RegionMapper),
			new CultureMapper(irRegionMapper, ck3RegionMapper, cultures),
			cultures,
			new TraitMapper(),
			new NicknameMapper(),
			new ProvinceMapper(),
			new DeathReasonMapper(),
			new DNAFactory(irModFS, ck3ModFS),
			endDate,
			configuration);

		Assert.Collection(ck3Characters.OrderBy(c => c.Id),
			ck3Male => {
				Assert.Equal(new Date(899, 3, 27, AUC: true),
					ck3Male.History.Fields["spouses"].DateToEntriesDict.FirstOrDefault().Key);
			},
			ck3Female => Assert.Equal("imperator2", ck3Female.Id)
		);
	}

	[Fact]
	public void OnlyEarlyPregnanciesAreImportedFromImperator() {
		var conversionDate = new Date(900, 2, 1, AUC: true);
		var configuration = new Configuration { CK3BookmarkDate = conversionDate };
		var imperatorWorld = new TestImperatorWorld(configuration);

		var male = new ImperatorToCK3.Imperator.Characters.Character(1);

		var female1Reader = new BufferedReader("female=yes unborn={ { mother=2 father=1 date=900.9.1 } }");
		// child will be born 7 months after conversion date, will be imported
		var female1 = ImperatorToCK3.Imperator.Characters.Character.Parse(female1Reader, "2", null);

		var female2Reader = new BufferedReader("female=yes unborn={ { mother=3 father=1 date=900.10.1 is_bastard=yes } }");
		// child will be born 8 months after conversion date, will be imported
		var female2 = ImperatorToCK3.Imperator.Characters.Character.Parse(female2Reader, "3", null);

		var female3Reader = new BufferedReader("female=yes unborn={ { mother=3 father=1 date=900.6.1 } }");
		// child will be born 4 months after conversion date, will not be imported
		var female3 = ImperatorToCK3.Imperator.Characters.Character.Parse(female3Reader, "4", null);

		imperatorWorld.Characters.Add(male);
		imperatorWorld.Characters.Add(female1);
		imperatorWorld.Characters.Add(female2);
		imperatorWorld.Characters.Add(female3);

		var landedTitles = new Title.LandedTitles();
		var ck3Religions = new ReligionCollection(landedTitles);
		var ck3RegionMapper = new CK3RegionMapper();
		var ck3Characters = new CharacterCollection();
		ck3Characters.ImportImperatorCharacters(
			imperatorWorld,
			new ReligionMapper(ck3Religions, irRegionMapper, ck3RegionMapper),
			new CultureMapper(irRegionMapper, ck3RegionMapper, cultures),
			cultures,
			new TraitMapper(),
			new NicknameMapper(),
			new ProvinceMapper(),
			new DeathReasonMapper(),
			new DNAFactory(irModFS, ck3ModFS),
			conversionDate,
			configuration);

		ck3Characters["imperator2"].Pregnancies
			.Should()
			.ContainEquivalentOf(new Pregnancy("imperator1", "imperator2", new Date(900, 9, 1, AUC: true), isBastard: false));
		ck3Characters["imperator3"].Pregnancies
			.Should()
			.ContainEquivalentOf(new Pregnancy("imperator1", "imperator3", new Date(900, 10, 1, AUC: true), isBastard: true));
		ck3Characters["imperator4"].Pregnancies.Should().BeEmpty();
	}

	[Fact]
	public void ImperatorCountriesGoldCanBeDistributedAmongRulerAndVassals() {
		var conversionDate = new Date(470, 2, 1, AUC: true);
		var config = new Configuration {
			ImperatorPath = "TestFiles/LandedTitlesTests/Imperator",
			CK3BookmarkDate = conversionDate,
			ImperatorCurrencyRate = 0.5 // 1 Imperator gold is worth 0.5 CK3 gold
		};

		var imperatorWorld = new TestImperatorWorld(config);

		imperatorWorld.Provinces.Add(new ImperatorToCK3.Imperator.Provinces.Province(1));
		// provinces for governorship 1
		imperatorWorld.Provinces.Add(new ImperatorToCK3.Imperator.Provinces.Province(2));
		imperatorWorld.Provinces.Add(new ImperatorToCK3.Imperator.Provinces.Province(3));
		// provinces for governorship 2
		imperatorWorld.Provinces.Add(new ImperatorToCK3.Imperator.Provinces.Province(4));
		imperatorWorld.Provinces.Add(new ImperatorToCK3.Imperator.Provinces.Province(5));
		imperatorWorld.Provinces.Add(new ImperatorToCK3.Imperator.Provinces.Province(6));

		var monarch = new ImperatorToCK3.Imperator.Characters.Character(1000);
		var governor1 = new ImperatorToCK3.Imperator.Characters.Character(1001);
		var governor2 = new ImperatorToCK3.Imperator.Characters.Character(1002);
		imperatorWorld.Characters.Add(monarch);
		imperatorWorld.Characters.Add(governor1);
		imperatorWorld.Characters.Add(governor2);

		var countryReader = new BufferedReader(@"
			tag=PRY
			country_name={ name=""PRY"" }
			capital=1
			currency_data={gold=200}
			ruler_term={ character=1000 start_date=440.10.1 }
		");
		var country = Country.Parse(countryReader, 589);
		Assert.Equal(200, country.Currencies.Gold);
		imperatorWorld.Countries.Add(country);
		imperatorWorld.Characters.LinkCountries(imperatorWorld.Countries);

		imperatorWorld.Areas.LoadAreas(imperatorWorld.ModFS, imperatorWorld.Provinces);
		Assert.True(imperatorWorld.Areas.ContainsKey("galatia_area"));
		Assert.True(imperatorWorld.Areas.ContainsKey("paphlagonia_area"));
		
		imperatorWorld.ImperatorRegionMapper.LoadRegions(imperatorWorld.ModFS, new ColorFactory());
		Assert.True(imperatorWorld.ImperatorRegionMapper.RegionNameIsValid("galatia_area"));
		Assert.True(imperatorWorld.ImperatorRegionMapper.RegionNameIsValid("paphlagonia_area"));
		Assert.True(imperatorWorld.ImperatorRegionMapper.RegionNameIsValid("galatia_region"));
		Assert.True(imperatorWorld.ImperatorRegionMapper.RegionNameIsValid("paphlagonia_region"));
		
		var ck3RegionMapper = new CK3RegionMapper();

		var governorshipReader1 = new BufferedReader(
			"who=589 " +
			"character=1001 " +
			"start_date=450.10.1 " +
			"governorship = \"galatia_region\""
		);
		var governorshipReader2 = new BufferedReader(
			"who=589 " +
			"character=1002 " +
			"start_date=450.10.1 " +
			"governorship = \"paphlagonia_region\""
		);
		var governorship1 = new Governorship(governorshipReader1, imperatorWorld.Countries, imperatorWorld.ImperatorRegionMapper);
		var governorship2 = new Governorship(governorshipReader2, imperatorWorld.Countries, imperatorWorld.ImperatorRegionMapper);
		imperatorWorld.JobsDB.Governorships.Add(governorship1);
		imperatorWorld.JobsDB.Governorships.Add(governorship2);

		var titles = new Title.LandedTitles();
		titles.LoadTitles(new BufferedReader(@"
			c_county1 = { b_barony1={province=1} }
			c_county2 = { b_barony2={province=2} }
			c_county3 = { b_barony3={province=3} }
			c_county4 = { b_barony4={province=4} }
			c_county5 = { b_barony5={province=5} }
			c_county6 = { b_barony6={province=6} }")
		);

		var tagTitleMapper = new TagTitleMapper();
		var provinceMapper = new ProvinceMapper();
		provinceMapper.LoadMappings(provinceMappingsPath, "test_version");
		
		var countryLocBlock = imperatorWorld.LocDB.AddLocBlock("PRY");
		countryLocBlock["english"] = "Phrygian Empire"; // this ensures that the CK3 title will be an empire

		var religionCollection = new ReligionCollection(titles);
		var religionMapper = new ReligionMapper(religionCollection, imperatorWorld.ImperatorRegionMapper, ck3RegionMapper);
		var cultureMapper = new CultureMapper(imperatorWorld.ImperatorRegionMapper, ck3RegionMapper, cultures);
		var coaMapper = new CoaMapper();
		var definiteFormMapper = new DefiniteFormMapper();
		var traitMapper = new TraitMapper();
		var nicknameMapper = new NicknameMapper();
		var deathReasonMapper = new DeathReasonMapper();

		// Import Imperator ruler and governors.
		var characters = new CharacterCollection();
		characters.ImportImperatorCharacters(
			imperatorWorld,
			religionMapper,
			cultureMapper,
			cultures,
			traitMapper,
			nicknameMapper,
			provinceMapper,
			deathReasonMapper,
			new DNAFactory(irModFS, ck3ModFS),
			conversionDate,
			config);

		// Import country 589.
		titles.ImportImperatorCountries(
			imperatorWorld.Countries,
			Array.Empty<Dependency>(),
			tagTitleMapper,
			imperatorWorld.LocDB,
			provinceMapper,
			coaMapper,
			new GovernmentMapper(ck3GovernmentIds: Array.Empty<string>()),
			new SuccessionLawMapper(),
			definiteFormMapper,
			religionMapper,
			cultureMapper,
			nicknameMapper,
			characters,
			conversionDate,
			config,
			new List<KeyValuePair<Country, Dependency?>>());

		var provinces = new ProvinceCollection(ck3ModFS);
		provinces.ImportImperatorProvinces(imperatorWorld, titles, cultureMapper, religionMapper, provinceMapper, conversionDate, config);

		titles.ImportImperatorGovernorships(
			imperatorWorld,
			provinces,
			tagTitleMapper,
			imperatorWorld.LocDB,
			config,
			provinceMapper,
			definiteFormMapper,
			imperatorWorld.ImperatorRegionMapper,
			coaMapper,
			countyLevelGovernorships: new List<Governorship>());

		var ck3Country = titles["e_IRTOCK3_PRY"];
		Assert.Equal("imperator1000", ck3Country.GetHolderId(conversionDate));

		characters.DistributeCountriesGold(titles, config);
		// Due to 0.5 currency rate, from Imperator country's 200 gold we have 100 CK3 gold.
		// Gold is divided among ruler and vassals, with ruler having weight of 2.
		// So from 100 gold, ruler gets 50 and both governor-vassals get 25 each.
		Assert.Collection(characters.OrderBy(c => c.Id),
			ck3Monarch => {
				Assert.Equal("imperator1000", ck3Monarch.Id);
				Assert.Equal(50, ck3Monarch.Gold);
			},
			ck3Vassal1 => {
				Assert.Equal("imperator1001", ck3Vassal1.Id);
				Assert.Equal(25, ck3Vassal1.Gold);
			},
			ck3Vassal2 => {
				Assert.Equal("imperator1002", ck3Vassal2.Id);
				Assert.Equal(25, ck3Vassal2.Gold);
			});
	}
}