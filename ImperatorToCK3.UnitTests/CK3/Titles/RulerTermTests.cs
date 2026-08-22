using commonItems;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.UnitTests.TestHelpers;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.UnitTests.CK3.Titles;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class RulerTermTests {
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly MapData irMapData = new(irModFS);
	private static readonly ImperatorRegionMapper irRegionMapper;
	private const string CK3Root = "TestFiles/CK3/game";
	private readonly ModFilesystem ck3ModFs = new(CK3Root, Array.Empty<Mod>());
	private static readonly TestCK3CultureCollection cultures = new();

	static RulerTermTests() {
		var irProvinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection {new(1), new(2), new(3)};
		AreaCollection areas = new();
		areas.LoadAreas(irModFS, irProvinces);
		irRegionMapper = new ImperatorRegionMapper(areas, irMapData);
		irRegionMapper.LoadRegions(irModFS, new ColorFactory());
		
		cultures.GenerateTestCulture("greek");
	}

	[Fact]
	public void ImperatorRulerTermIsCorrectlyConverted() {
		var reader = new BufferedReader(
			"character = 69 " +
			"start_date = 500.2.3 " +
			"government = dictatorship"
		);
		var impRulerTerm = ImperatorToCK3.Imperator.Countries.RulerTerm.Parse(reader);
		Directory.CreateDirectory("configurables");
		File.WriteAllText("configurables/government_map.txt", "link = {ir=dictatorship ck3=feudal_government }");
		var govMapper = new GovernmentMapper(ck3GovernmentIds: ["feudal_government"]);
		File.Delete("configurables/government_map.txt"); // cleanup
		var landedTitles = new Title.LandedTitles();
		var ck3Religions = new ReligionCollection(landedTitles);
		var ck3RegionMapper = new CK3RegionMapper();
		var colorFactory = new ColorFactory();
		var ck3ModFlags = new OrderedDictionary<string, bool>();
		
		var testTitle = landedTitles.Add("k_test_title");
		var ck3RulerTerm = new RulerTerm(
			testTitle,
			impRulerTerm,
			new ImperatorToCK3.CK3.Characters.CharacterCollection(),
			govMapper,
			new LocDB("english"),
			new TestCK3LocDB(),
			new ReligionMapper(ck3Religions, irRegionMapper, ck3RegionMapper),
			new CultureMapper(irRegionMapper, ck3RegionMapper, new CultureCollection(colorFactory, new PillarCollection(colorFactory, ck3ModFlags), ck3ModFlags)),
			new NicknameMapper("TestFiles/configurables/nickname_map.txt"),
			new ProvinceMapper(),
			new Configuration(),
			enabledCK3Dlcs: Array.Empty<string>()
		);
		Assert.Equal("imperator69", ck3RulerTerm.CharacterId);
		Assert.Equal(new Date(500, 2, 3, AUC: true), ck3RulerTerm.StartDate);
		Assert.Equal("feudal_government", ck3RulerTerm.Government);
	}

	[Fact]
	public void PreImperatorTermIsCorrectlyConverted() {
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		var countryReader = new BufferedReader("= { tag = SPA capital=420 }");
		var sparta = ImperatorToCK3.Imperator.Countries.Country.Parse(countryReader, 69);
		countries.Add(sparta);

		var preImpTermReader = new BufferedReader("= { name=\"Alexander\"" +
			" birth_date=200.1.1 death_date=300.1.1 throne_date=250.1.1" +
			" nickname=stupid religion=hellenic culture=spartan" +
			" country=SPA }"
		);
		var impRulerTerm = new ImperatorToCK3.Imperator.Countries.RulerTerm(preImpTermReader, countries);

		var landedTitles = new Title.LandedTitles();
		var ck3Religions = new ReligionCollection(landedTitles);
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		Directory.CreateDirectory("configurables");
		File.WriteAllText("configurables/government_map.txt", "link = {ir=dictatorship ck3=feudal_government }");
		var govMapper = new GovernmentMapper(ck3GovernmentIds: ["feudal_government"]);
		File.Delete("configurables/government_map.txt"); // cleanup
		var ck3RegionMapper = new CK3RegionMapper();
		var religionMapper = new ReligionMapper(
			new BufferedReader("link={ir=hellenic ck3=hellenic}"),
			ck3Religions,
			irRegionMapper,
			ck3RegionMapper
		);
		var ck3Characters = new ImperatorToCK3.CK3.Characters.CharacterCollection();
		
		var testTitle = landedTitles.Add("k_test_title");
		var ck3RulerTerm = new RulerTerm(
			testTitle,
			impRulerTerm,
			ck3Characters,
			govMapper,
			new LocDB("english"),
			new TestCK3LocDB(),
			religionMapper,
			new CultureMapper(new BufferedReader("link = { ir=spartan ck3=greek }"), irRegionMapper, ck3RegionMapper, cultures),
			new NicknameMapper("TestFiles/configurables/nickname_map.txt"),
			new ProvinceMapper(),
			new Configuration(),
			enabledCK3Dlcs: Array.Empty<string>()
		);
		Assert.NotNull(ck3RulerTerm.CharacterId);
		Assert.Equal("imperatorRegnalSPAAlexander504_1_1BC", ck3RulerTerm.CharacterId);
		Assert.Equal(new Date(250, 1, 1, AUC: true), ck3RulerTerm.StartDate);

		var conversionDate = new Date(1000, 1, 1);
		var ck3Character = ck3Characters["imperatorRegnalSPAAlexander504_1_1BC"];
		Assert.Equal("-554.1.1", ck3Character.BirthDate);
		Assert.Equal("-454.1.1", ck3Character.DeathDate);
		Assert.Equal("Alexander", ck3Character.GetName(conversionDate));
		Assert.Equal("dull", ck3Character.GetNickname(conversionDate));
		Assert.Equal("greek", ck3Character.GetCultureId(conversionDate));
		Assert.Equal("hellenic", ck3Character.GetFaithId(conversionDate));
	}

	[Fact]
	public void GovernmentStaysNullWhenImperatorGovernmentIsNull() {
		var reader = new BufferedReader(
			"character = 69 " +
			"start_date = 500.2.3"
		); // no government keyword
		var impRulerTerm = ImperatorToCK3.Imperator.Countries.RulerTerm.Parse(reader);

		var ck3RulerTerm = CreateCk3RulerTerm(impRulerTerm, new ImperatorToCK3.CK3.Characters.CharacterCollection());

		Assert.Equal("imperator69", ck3RulerTerm.CharacterId);
		Assert.Equal(new Date(500, 2, 3, AUC: true), ck3RulerTerm.StartDate);
		Assert.Null(ck3RulerTerm.Government);
	}

	[Fact]
	public void CharacterIdStaysNullWhenPreImperatorRulerHasNoDeathDate() {
		var impRulerTerm = CreatePreImperatorRulerTerm(
			"= { name=\"Alexander\"" +
			" birth_date=200.1.1" +
			" throne_date=250.1.1" +
			" country=SPA }" // no death_date
		);
		var ck3Characters = new ImperatorToCK3.CK3.Characters.CharacterCollection();

		var ck3RulerTerm = CreateCk3RulerTerm(impRulerTerm, ck3Characters);

		Assert.Null(ck3RulerTerm.CharacterId);
		Assert.Empty(ck3Characters);
	}

	[Fact]
	public void CharacterIdStaysNullWhenPreImperatorRulerHasNoCountry() {
		var impRulerTerm = CreatePreImperatorRulerTerm(
			"= { name=\"Alexander\"" +
			" birth_date=200.1.1" +
			" death_date=300.1.1" +
			" throne_date=250.1.1 }" // no country
		);
		var ck3Characters = new ImperatorToCK3.CK3.Characters.CharacterCollection();

		var ck3RulerTerm = CreateCk3RulerTerm(impRulerTerm, ck3Characters);

		Assert.Null(ck3RulerTerm.CharacterId);
		Assert.Empty(ck3Characters);
	}

	[Fact]
	public void WarningIsLoggedWhenNewRulerCharacterIdAlreadyInUse() {
		var impRulerTerm = CreatePreImperatorRulerTerm(
			"= { name=\"Alexander\"" +
			" birth_date=200.1.1 death_date=300.1.1 throne_date=250.1.1" +
			" nickname=stupid religion=hellenic culture=spartan" +
			" country=SPA }"
		);
		var ck3Characters = new ImperatorToCK3.CK3.Characters.CharacterCollection();

		// The ID of the character that would be created for the pre-Imperator ruler.
		var expectedCharacterId = $"imperatorRegnal{impRulerTerm.PreImperatorRuler!.Country!.Tag}" +
			$"{impRulerTerm.PreImperatorRuler.Name}{impRulerTerm.StartDate.ToString()[1..]}BC".Replace('.', '_');
		var existingCharacter = new ImperatorToCK3.CK3.Characters.Character(
			expectedCharacterId,
			name: "ExistingName",
			birthDate: new Date(1, 1, 1),
			characters: ck3Characters
		);
		ck3Characters.Add(existingCharacter);

		var output = new StringWriter();
		Console.SetOut(output);
		var ck3RulerTerm = CreateCk3RulerTerm(impRulerTerm, ck3Characters);

		var logStr = output.ToString();
		Assert.Contains($"[WARN] Cannot add pre-Imperator ruler {expectedCharacterId} " +
		                "- a character with this ID already exists!", logStr);
		Assert.Null(ck3RulerTerm.CharacterId);
		Assert.Single(ck3Characters); // the pre-existing character, no new one added
	}

	private static ImperatorToCK3.Imperator.Countries.RulerTerm CreatePreImperatorRulerTerm(string content) {
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		var sparta = ImperatorToCK3.Imperator.Countries.Country.Parse(
			new BufferedReader("= { tag = SPA capital=420 }"),
			69
		);
		countries.Add(sparta);

		return new ImperatorToCK3.Imperator.Countries.RulerTerm(new BufferedReader(content), countries);
	}

	private ImperatorToCK3.CK3.Titles.RulerTerm CreateCk3RulerTerm(
		ImperatorToCK3.Imperator.Countries.RulerTerm impRulerTerm,
		ImperatorToCK3.CK3.Characters.CharacterCollection ck3Characters
	) {
		Directory.CreateDirectory("configurables");
		File.WriteAllText("configurables/government_map.txt", "link = {ir=dictatorship ck3=feudal_government }");
		var govMapper = new GovernmentMapper(ck3GovernmentIds: ["feudal_government"]);
		File.Delete("configurables/government_map.txt"); // cleanup

		var landedTitles = new Title.LandedTitles();
		var ck3Religions = new ReligionCollection(landedTitles);
		ck3Religions.LoadReligions(ck3ModFs, new ColorFactory());
		var ck3RegionMapper = new CK3RegionMapper();
		var religionMapper = new ReligionMapper(
			new BufferedReader("link={ir=hellenic ck3=hellenic}"),
			ck3Religions,
			irRegionMapper,
			ck3RegionMapper
		);

		var testTitle = landedTitles.Add("k_test_title");
		return new ImperatorToCK3.CK3.Titles.RulerTerm(
			testTitle,
			impRulerTerm,
			ck3Characters,
			govMapper,
			new LocDB("english"),
			new TestCK3LocDB(),
			religionMapper,
			new CultureMapper(
				new BufferedReader("link = { ir=spartan ck3=greek }"),
				irRegionMapper,
				ck3RegionMapper,
				cultures
			),
			new NicknameMapper("TestFiles/configurables/nickname_map.txt"),
			new ProvinceMapper(),
			new Configuration(),
			enabledCK3Dlcs: Array.Empty<string>()
		);
	}
}