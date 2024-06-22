using commonItems;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Diplomacy;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Imperator.Provinces;
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
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using ProvinceCollection = ImperatorToCK3.CK3.Provinces.ProvinceCollection;

namespace ImperatorToCK3.UnitTests.CK3.Titles;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class LandedTitlesTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly MapData irMapData = new(irModFS);
	private static readonly ImperatorRegionMapper irRegionMapper;
	private readonly string provinceMappingsPath = "TestFiles/LandedTitlesTests/province_mappings.txt";
	private const string CK3Root = "TestFiles/LandedTitlesTests/CK3/game";
	private readonly ModFilesystem ck3ModFS = new(CK3Root, new List<Mod>());
	private readonly Configuration defaultConfig = new() { ImperatorCivilizationWorth = 0.4 };
	private readonly CultureCollection cultures;
	
	static LandedTitlesTests() {
		ImperatorToCK3.Imperator.Provinces.ProvinceCollection irProvinces = new();
		irProvinces.LoadProvinces(
			new BufferedReader("1={} 2={} 3={} 4={} 5={} 6={} 7={} 8={} 9={} 69={}"),
			new StateCollection(),
			new CountryCollection()
		);
		AreaCollection areas = new();
		areas.LoadAreas(irModFS, irProvinces);
		irRegionMapper = new ImperatorRegionMapper(areas, irMapData);
		irRegionMapper.LoadRegions(irModFS, new ColorFactory());
	}

	public LandedTitlesTests() {
		var colorFactory = new ColorFactory();
		var ck3ModFlags = Array.Empty<string>();
		PillarCollection pillars = new(colorFactory, ck3ModFlags);
		cultures = new CultureCollection(colorFactory, pillars, ck3ModFlags);
	}

	[Fact]
	public void TitlesDefaultToEmpty() {
		var reader = new BufferedReader(string.Empty);
		var titles = new Title.LandedTitles();
		titles.LoadTitles(reader);

		Assert.Empty(titles);
	}

	[Fact]
	public void TitlesCanBeLoaded() {
		var reader = new BufferedReader(
			"b_barony = { province = 12 }\n" +
			"c_county = { landless = yes }\n"
		);

		var titles = new Title.LandedTitles();
		titles.LoadTitles(reader);

		var barony = titles["b_barony"];
		var county = titles["c_county"];

		Assert.Equal(2, titles.Count);
		Assert.Equal((ulong)12, barony.ProvinceId);
		Assert.True(county.Landless);
	}

	[Fact]
	public void TitlesCanBeLoadedRecursively() {
		var reader = new BufferedReader(
			"e_empire1 = { k_kingdom2 = { d_duchy3 = { b_barony4 = { province = 12 } } } }\n" +
			"c_county5 = { landless = yes }\n"
		);

		var titles = new Title.LandedTitles();
		titles.LoadTitles(reader);

		var barony = titles["b_barony4"];
		var county = titles["c_county5"];

		Assert.Equal(5, titles.Count);
		Assert.Equal((ulong)12, barony.ProvinceId);
		Assert.True(county.Landless);
	}

	[Fact]
	public void TitlesCanBeOverriddenByMods() {
		var reader = new BufferedReader(
			"e_empire1 = { k_kingdom2 = { d_duchy3 = { b_barony4 = { province = 12 } } } }\n" +
			"c_county5 = { landless = yes }\n"
		);

		var titles = new Title.LandedTitles();
		titles.LoadTitles(reader);

		var reader2 = new BufferedReader(
			"b_barony4 = { province = 15 }\n" +
			"c_county5 = { landless = no }\n"
		);
		titles.LoadTitles(reader2);

		var barony = titles["b_barony4"];
		var county = titles["c_county5"];

		Assert.Equal(5, titles.Count);
		Assert.Equal((ulong)15, barony.ProvinceId);
		Assert.False(county.Landless);
	}

	[Fact]
	public void TitlesCanBeAddedByMods() {
		var reader = new BufferedReader(
			"e_empire1 = { k_kingdom2 = { d_duchy3 = { b_barony4 = { province = 12 } } } }\n" +
			"c_county5 = { landless = yes }\n"
		);

		var titles = new Title.LandedTitles();
		titles.LoadTitles(reader);

		var reader2 = new BufferedReader(
			"c_county5 = { landless = no }\n" + // Overrides existing instance
			"e_empire6 = { k_kingdom7 = { d_duchy8 = { b_barony9 = { province = 12 } } } }\n" +
			"c_county10 = { landless = yes }\n"
		);
		titles.LoadTitles(reader2);

		Assert.Equal(10, titles.Count);
	}

	[Fact]
	public void CapitalsAreLinked() {
		var reader = new BufferedReader(
			"e_empire = {" +
			"\tcapital=c_county " +
			"\tk_kingdom = { d_duchy = { c_county = { b_barony = { province = 12 } } } } " +
			"}"
		);
		var titles = new Title.LandedTitles();
		titles.LoadTitles(reader);

		var empire = titles["e_empire"];
		var capitalCounty = empire.CapitalCounty;
		Assert.NotNull(capitalCounty);
		Assert.Equal("c_county", capitalCounty.Id);
		Assert.Equal("c_county", empire.CapitalCountyId);
	}

	[Fact]
	public void StaticTitlesCanMerge() {
		var reader = new BufferedReader(
			"e_empire = {" +
			"\tcapital=c_county " +
			"\tk_kingdom = { d_duchy = { c_county = { b_barony = { province = 12 } } } }" +
			"}" +
			"d_duchy2 = { c_county2 = { b_barony2 = { province = 14 } } }"
		);
		var titles = new Title.LandedTitles();
		titles.LoadTitles(reader);

		var reader2 = new BufferedReader(
			"e_empire = {" +
			"\tcapital=c_county " +
			"\tk_kingdom = { d_duchy2 = { } }" +
			"}"
		);
		var overrides = new Title.LandedTitles();
		overrides.LoadTitles(reader2);

		titles.CarveTitles(overrides);

		var kingdom = titles["k_kingdom"];
		Assert.Equal(2, kingdom.DeJureVassals.Count);
	}
	[Fact]
	public void StaticTitlesCanCarve() {
		var reader = new BufferedReader(
			"e_empire = {" +
			"\tcapital=c_county " +
			"\tk_kingdom = { d_duchy = { c_county = { b_barony = { province = 12 } } } } " +
			"}"
		);
		var titles = new Title.LandedTitles();
		titles.LoadTitles(reader);

		var reader2 = new BufferedReader(
			"e_empire = {" +
			"\tcapital=c_county " +
			"\tk_kingdom2 = { d_duchy = { } } " +
			"}"
		);
		var overrides = new Title.LandedTitles();
		overrides.LoadTitles(reader2);

		titles.CarveTitles(overrides);

		var kingdom0 = titles["k_kingdom"];
		var kingdom2 = titles["k_kingdom2"];
		Assert.Empty(kingdom0.DeJureVassals);
		Assert.Single(kingdom2.DeJureVassals);
	}

	[Fact]
	public void GovernorshipsCanBeRecognizedAsCountyLevel() {
		var config = new Configuration { ImperatorPath = "TestFiles/LandedTitlesTests/Imperator" };
		var imperatorWorld = new TestImperatorWorld(config);

		imperatorWorld.Provinces.Add(new Province(1));
		imperatorWorld.Provinces.Add(new Province(2));
		imperatorWorld.Provinces.Add(new Province(3));

		var governor = new ImperatorToCK3.Imperator.Characters.Character(25212);
		imperatorWorld.Characters.Add(governor);

		var countryReader = new BufferedReader("tag=PRY capital=1");
		var country = Country.Parse(countryReader, 589);
		imperatorWorld.Countries.Add(country);

		imperatorWorld.Areas.LoadAreas(imperatorWorld.ModFS, imperatorWorld.Provinces);
		var irRegionMapper = new ImperatorRegionMapper(imperatorWorld.Areas, irMapData);
		irRegionMapper.LoadRegions(imperatorWorld.ModFS, new ColorFactory());
		Assert.True(irRegionMapper.RegionNameIsValid("galatia_area"));
		Assert.True(irRegionMapper.RegionNameIsValid("galatia_region"));
		var ck3RegionMapper = new CK3RegionMapper();

		var reader = new BufferedReader(
			"who=589 " +
			"character=25212 " +
			"start_date=450.10.1 " +
			"governorship = \"galatia_region\""
		);
		var governorship1 = new Governorship(reader, imperatorWorld.Countries, irRegionMapper);
		imperatorWorld.JobsDB.Governorships.Add(governorship1);
		var titles = new Title.LandedTitles();
		titles.LoadTitles(new BufferedReader(
			"c_county1 = { b_barony1={province=1} } " +
			"c_county2 = { b_barony2={province=2} } " +
			"c_county3 = { b_barony3={province=3} }")
		);
		var countyLevelGovernorships = new List<Governorship>();

		var tagTitleMapper = new TagTitleMapper();
		var provinceMapper = new ProvinceMapper();
		provinceMapper.LoadMappings(provinceMappingsPath, "test_version");
		var locDB = new LocDB("english");
		var ck3Religions = new ReligionCollection(titles);
		var religionMapper = new ReligionMapper(ck3Religions, irRegionMapper, ck3RegionMapper);
		var cultureMapper = new CultureMapper(irRegionMapper, ck3RegionMapper, cultures);
		var coaMapper = new CoaMapper();
		var definiteFormMapper = new DefiniteFormMapper();
		var traitMapper = new TraitMapper();
		var nicknameMapper = new NicknameMapper();
		var deathReasonMapper = new DeathReasonMapper();
		var dnaFactory = new DNAFactory(irModFS, ck3ModFS);
		var conversionDate = new Date(500, 1, 1);

		// Import Imperator governor.
		var characters = new ImperatorToCK3.CK3.Characters.CharacterCollection();
		characters.ImportImperatorCharacters(
			imperatorWorld,
			religionMapper,
			cultureMapper,
			cultures,
			traitMapper,
			nicknameMapper,
			provinceMapper,
			deathReasonMapper,
			dnaFactory,
			conversionDate,
			config
		);

		// Import country 589.
		titles.ImportImperatorCountries(imperatorWorld.Countries, imperatorWorld.Dependencies, tagTitleMapper, locDB, provinceMapper, coaMapper, new GovernmentMapper(ck3GovernmentIds: Array.Empty<string>()), new SuccessionLawMapper(), definiteFormMapper, religionMapper, cultureMapper, nicknameMapper, characters, conversionDate, config, new List<KeyValuePair<Country, Dependency?>>());
		Assert.Collection(titles,
			title => Assert.Equal("c_county1", title.Id),
			title => Assert.Equal("b_barony1", title.Id),
			title => Assert.Equal("c_county2", title.Id),
			title => Assert.Equal("b_barony2", title.Id),
			title => Assert.Equal("c_county3", title.Id),
			title => Assert.Equal("b_barony3", title.Id),
			title => Assert.Equal("d_IRTOCK3_PRY", title.Id)
		);

		var provinces = new ProvinceCollection(ck3ModFS);
		provinces.ImportImperatorProvinces(imperatorWorld, titles, cultureMapper, religionMapper, provinceMapper, conversionDate, config);
		// Country 589 is imported as duchy-level title, so its governorship of galatia_region will be county level.
		titles.ImportImperatorGovernorships(imperatorWorld, provinces, tagTitleMapper, locDB, config, provinceMapper, definiteFormMapper, irRegionMapper, coaMapper, countyLevelGovernorships);

		Assert.Collection(titles,
			title => Assert.Equal("c_county1", title.Id),
			title => Assert.Equal("b_barony1", title.Id),
			title => Assert.Equal("c_county2", title.Id),
			title => Assert.Equal("b_barony2", title.Id),
			title => Assert.Equal("c_county3", title.Id),
			title => Assert.Equal("b_barony3", title.Id),
			title => Assert.Equal("d_IRTOCK3_PRY", title.Id)
		// Governorship is not added as a new title.
		);
		Assert.Collection(countyLevelGovernorships,
			clg1 => {
				Assert.Equal("galatia_region", clg1.Region.Id);
				Assert.Equal((ulong)589, clg1.Country.Id);
				Assert.Equal((ulong)25212, clg1.CharacterId);
			}
		);
	}

	[Fact]
	public void DevelopmentIsNotChangedForCountiesOutsideOfImperatorMap() {
		var date = new Date(476, 1, 1);
		var titles = new Title.LandedTitles();
		var county = titles.Add("c_county");
		county.SetDevelopmentLevel(33, date);

		var ck3Provinces = new ProvinceCollection();

		titles.ImportDevelopmentFromImperator(ck3Provinces, date, defaultConfig.ImperatorCivilizationWorth);

		Assert.Equal(33, county.GetDevelopmentLevel(date));
	}

	[Fact]
	public void DevelopmentIsCorrectlyCalculatedFor1ProvinceTo1BaronyCountyMapping() {
		var conversionDate = new Date(476, 1, 1);
		var config = new Configuration { CK3BookmarkDate = conversionDate };
		var titles = new Title.LandedTitles();
		var titlesReader = new BufferedReader(
			"c_county1={ b_barony1={province=1} }"
		);
		titles.LoadTitles(titlesReader);

		var irWorld = new TestImperatorWorld(config);
		var irProvince = new ImperatorToCK3.Imperator.Provinces.Province(1) { CivilizationValue = 25 };
		irWorld.Provinces.Add(irProvince);

		var provinceMapper = new ProvinceMapper();
		provinceMapper.LoadMappings(provinceMappingsPath, "test_version");

		var ck3Provinces = new ProvinceCollection { new(1), new(2), new(3) };
		var ck3RegionMapper = new CK3RegionMapper();
		var cultureMapper = new CultureMapper(irRegionMapper, ck3RegionMapper, cultures);
		var religions = new ReligionCollection(titles);
		var religionMapper = new ReligionMapper(religions, irRegionMapper, ck3RegionMapper);
		ck3Provinces.ImportImperatorProvinces(irWorld, titles, cultureMapper, religionMapper, provinceMapper, conversionDate, config);

		var date = config.CK3BookmarkDate;
		titles.ImportDevelopmentFromImperator(ck3Provinces, date, defaultConfig.ImperatorCivilizationWorth);

		Assert.Equal(8, titles["c_county1"].GetDevelopmentLevel(date)); // 0.4*(25-sqrt(25)) ≈ 8
	}

	[Fact]
	public void DevelopmentFromImperatorProvinceCanBeUsedForMultipleCK3Provinces() {
		var conversionDate = new Date(476, 1, 1);
		var config = new Configuration { CK3BookmarkDate = conversionDate };
		var titles = new Title.LandedTitles();
		var titlesReader = new BufferedReader(
			"c_county1={ b_barony1={province=1} } " +
			"c_county2={ b_barony2={province=2} } " +
			"c_county3={ b_barony3={province=3} }"
		);
		titles.LoadTitles(titlesReader);

		var irWorld = new TestImperatorWorld(config);
		var irProvince = new ImperatorToCK3.Imperator.Provinces.Province(1) { CivilizationValue = 21 };
		irWorld.Provinces.Add(irProvince);

		var provinceMapper = new ProvinceMapper();
		provinceMapper.LoadMappings("TestFiles/LandedTitlesTests/province_mappings_1_to_3.txt", "test_version");

		var ck3Provinces = new ProvinceCollection { new(1), new(2), new(3) };
		var ck3RegionMapper = new CK3RegionMapper();
		var cultureMapper = new CultureMapper(irRegionMapper, ck3RegionMapper, cultures);
		var religions = new ReligionCollection(titles);
		var religionMapper = new ReligionMapper(religions, irRegionMapper, ck3RegionMapper);
		ck3Provinces.ImportImperatorProvinces(irWorld, titles, cultureMapper, religionMapper, provinceMapper, conversionDate, config);

		var date = config.CK3BookmarkDate;
		titles.ImportDevelopmentFromImperator(ck3Provinces, date, defaultConfig.ImperatorCivilizationWorth);

		Assert.Equal(6, titles["c_county1"].GetDevelopmentLevel(date)); // 0.4 * (21-sqrt(21) ≈ 6
		Assert.Equal(6, titles["c_county2"].GetDevelopmentLevel(date)); // same as above
		Assert.Equal(6, titles["c_county3"].GetDevelopmentLevel(date)); // same as above
	}

	[Fact]
	public void DevelopmentOfCountyIsCalculatedFromAllCountyProvinces() {
		var conversionDate = new Date(476, 1, 1);
		var config = new Configuration { CK3BookmarkDate = conversionDate };
		var titles = new Title.LandedTitles();
		var titlesReader = new BufferedReader(
			"c_county1={ b_barony1={province=1} b_barony2={province=2} }"
		);
		titles.LoadTitles(titlesReader);

		var irWorld = new TestImperatorWorld(config);
		var irProvince1 = new ImperatorToCK3.Imperator.Provinces.Province(1) { CivilizationValue = 10 };
		irWorld.Provinces.Add(irProvince1);
		var irProvince2 = new ImperatorToCK3.Imperator.Provinces.Province(2) { CivilizationValue = 40 };
		irWorld.Provinces.Add(irProvince2);

		var provinceMapper = new ProvinceMapper();
		provinceMapper.LoadMappings(provinceMappingsPath, "test_version");

		var ck3Provinces = new ProvinceCollection { new(1), new(2), new(3) };
		var ck3RegionMapper = new CK3RegionMapper();
		var cultureMapper = new CultureMapper(irRegionMapper, ck3RegionMapper, cultures);
		var religions = new ReligionCollection(titles);
		var religionMapper = new ReligionMapper(religions, irRegionMapper, ck3RegionMapper);
		ck3Provinces.ImportImperatorProvinces(irWorld, titles, cultureMapper, religionMapper, provinceMapper, conversionDate, config);

		var date = config.CK3BookmarkDate;
		titles.ImportDevelopmentFromImperator(ck3Provinces, date, defaultConfig.ImperatorCivilizationWorth);
		
		// Dev from province 1: 10-sqrt(10) ≈ 6
		// Dev from province 2: 40-sqrt(40) ≈ 33
		// Average: (6+33)/2 ≈ 19
		// Average multiplied by civilization worth: 0.4*19 ≈ 8
		Assert.Equal(8, titles["c_county1"].GetDevelopmentLevel(date));
	}

	[Fact]
	public void DerivedColorsHaveCorrectComponents() {
		var titles = new Title.LandedTitles();
		var baseColor = new Color(0.2, 0.3, 0.4);

		var baseTitle = titles.Add("e_base");
		baseTitle.Color1 = baseColor;

		var derivedTitle1 = titles.Add("k_derived1");
		var derivedColor1 = titles.GetDerivedColor(baseColor);
		derivedTitle1.Color1 = derivedColor1;

		var derivedTitle2 = titles.Add("k_derived2");
		var derivedColor2 = titles.GetDerivedColor(baseColor);
		derivedTitle2.Color1 = derivedColor2;

		Assert.Equal(baseColor.H, derivedColor1.H);
		Assert.Equal(baseColor.S, derivedColor1.S);
		Assert.NotEqual(baseColor.V, derivedColor1.V);

		Assert.Equal(baseColor.H, derivedColor2.H);
		Assert.Equal(baseColor.S, derivedColor2.S);
		Assert.NotEqual(baseColor.V, derivedColor2.V);

		Assert.NotEqual(derivedColor1.V, derivedColor2.V);
	}

	[Fact]
	public void WarningIsLoggedWhenColorCanNotBeDerived() {
		var titles = new Title.LandedTitles();
		var baseColor = new Color(0.2, 0.3, 0.4);
		var baseTitle = titles.Add("e_base");
		baseTitle.Color1 = baseColor;

		for (double v = 0; v <= 1; v += 0.01) {
			var color = new Color(baseColor.H, baseColor.S, v);
			var title = titles.Add($"k_{color.OutputHex()}");
			title.Color1 = color;
		}

		var logWriter = new StringWriter();
		Console.SetOut(logWriter);
		_ = titles.GetDerivedColor(baseColor);
		Assert.Contains("Couldn't generate new color from base", logWriter.ToString());
	}

	[Fact]
	public void HistoryCanBeLoadedFromInitialValues() {
		var date = new Date(867, 1, 1);
		var config = new Configuration {
			CK3BookmarkDate = date,
			CK3Path = "TestFiles/LandedTitlesTests/CK3"
		};
		var ck3ModFS = new ModFilesystem(Path.Combine(config.CK3Path, "game"), new List<Mod>());

		var titles = new Title.LandedTitles();
		var title = titles.Add("k_rome");

		titles.LoadHistory(config, ck3ModFS);

		Assert.Equal("67", title.GetHolderId(date));
		Assert.Equal("e_italia", title.GetLiegeId(date));
	}

	[Fact]
	public void HistoryIsLoadedFromDatedBlocks() {
		var date = new Date(867, 1, 1);
		var config = new Configuration {
			CK3BookmarkDate = date,
			CK3Path = "TestFiles/LandedTitlesTests/CK3"
		};
		var ck3ModFS = new ModFilesystem(Path.Combine(config.CK3Path, "game"), new List<Mod>());

		var titles = new Title.LandedTitles();
		var title = titles.Add("k_greece");

		titles.LoadHistory(config, ck3ModFS);

		Assert.Equal("420", title.GetHolderId(date));
		Assert.Equal(20, title.GetDevelopmentLevel(date));
	}

	[Fact]
	public void GetBaronyForProvinceReturnsCorrectBaronyOrNullWhenNotFound() {
		var titles = new Title.LandedTitles();
		var titlesReader = new BufferedReader(@"
				c_county = {
					b_barony1 = { province=1 }
					b_barony2 = { province=2 }
					b_barony3 = { province=3 }
				}");
		titles.LoadTitles(titlesReader);

		Assert.Equal("b_barony1", titles.GetBaronyForProvince(1)?.Id);
		Assert.Equal("b_barony2", titles.GetBaronyForProvince(2)?.Id);
		Assert.Equal("b_barony3", titles.GetBaronyForProvince(3)?.Id);
		Assert.Null(titles.GetBaronyForProvince(4));
	}
}