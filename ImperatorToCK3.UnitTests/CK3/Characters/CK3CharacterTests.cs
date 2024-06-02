using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using FluentAssertions;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Cultures;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.Trait;
using ImperatorToCK3.UnitTests.Mappers.Trait;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Culture = ImperatorToCK3.Imperator.Cultures.Culture;

namespace ImperatorToCK3.UnitTests.CK3.Characters;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CK3CharacterTests {
	private static readonly Date ConversionDate = new(867, 1, 1);
	private const string ImperatorRoot = "TestFiles/Imperator/game";
	private static readonly ModFilesystem IRModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly MapData irMapData = new(IRModFS);
	private static readonly ImperatorRegionMapper IRRegionMapper;
	private static readonly CultureMapper CultureMapper;
	private const string CK3Path = "TestFiles/CK3";
	private const string CK3Root = "TestFiles/CK3/game";
	private static readonly ModFilesystem CK3ModFS = new(CK3Root, Array.Empty<Mod>());
	private static readonly DNAFactory DNAFactory = new(IRModFS, CK3ModFS);
	private static TestCK3CultureCollection cultures = new();
	
	static CK3CharacterTests() {
		var irProvinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection {new(1), new(2), new(3)};
		AreaCollection areas = new();
		areas.LoadAreas(IRModFS, irProvinces);
		IRRegionMapper = new ImperatorRegionMapper(areas, irMapData);
		IRRegionMapper.LoadRegions(IRModFS, new ColorFactory());
			
		cultures.GenerateTestCulture("greek");
		cultures.GenerateTestCulture("macedonian");
		CultureMapper = new CultureMapper(IRRegionMapper, new CK3RegionMapper(), cultures);
	}

	public class CK3CharacterBuilder {
		private Configuration config = new() {
			CK3BookmarkDate = ConversionDate,
			CK3Path = CK3Path
		};

		private ImperatorToCK3.Imperator.Characters.Character imperatorCharacter = new(0);
		private CharacterCollection characters = new();
		private ReligionMapper religionMapper = new(new ReligionCollection(new Title.LandedTitles()), IRRegionMapper, new CK3RegionMapper());
		private CultureMapper cultureMapper = new(IRRegionMapper, new CK3RegionMapper(), cultures);
		private TraitMapper traitMapper = new("TestFiles/configurables/trait_map.txt", CK3ModFS);
		private NicknameMapper nicknameMapper = new("TestFiles/configurables/nickname_map.txt");
		private LocDB locDB = new("english");
		private ProvinceMapper provinceMapper = new();
		private DeathReasonMapper deathReasonMapper = new();

		public Character Build() {
			IRRegionMapper.LoadRegions(IRModFS, new ColorFactory());
			
			var character = new Character(
				imperatorCharacter,
				characters,
				religionMapper,
				cultureMapper,
				traitMapper,
				nicknameMapper,
				locDB,
				irMapData,
				provinceMapper,
				deathReasonMapper,
				DNAFactory,
				new Date(867, 1, 1),
				config,
				unlocalizedImperatorNames: new HashSet<string>()
			);
			return character;
		}
		public CK3CharacterBuilder WithImperatorCharacter(ImperatorToCK3.Imperator.Characters.Character imperatorCharacter) {
			this.imperatorCharacter = imperatorCharacter;
			return this;
		}
		public CK3CharacterBuilder WithCharacterCollection(CharacterCollection characters) {
			this.characters = characters;
			return this;
		}
		public CK3CharacterBuilder WithReligionMapper(ReligionMapper religionMapper) {
			this.religionMapper = religionMapper;
			return this;
		}
		public CK3CharacterBuilder WithCultureMapper(CultureMapper cultureMapper) {
			this.cultureMapper = cultureMapper;
			return this;
		}
		public CK3CharacterBuilder WithTraitMapper(TraitMapper traitMapper) {
			this.traitMapper = traitMapper;
			return this;
		}
		public CK3CharacterBuilder WithNicknameMapper(NicknameMapper nicknameMapper) {
			this.nicknameMapper = nicknameMapper;
			return this;
		}
		public CK3CharacterBuilder WithLocDB(LocDB locDB) {
			this.locDB = locDB;
			return this;
		}
		public CK3CharacterBuilder WithProvinceMapper(ProvinceMapper provinceMapper) {
			this.provinceMapper = provinceMapper;
			return this;
		}
		public CK3CharacterBuilder WithDeathReasonMapper(DeathReasonMapper deathReasonMapper) {
			this.deathReasonMapper = deathReasonMapper;
			return this;
		}
		public CK3CharacterBuilder WithConfiguration(Configuration config) {
			this.config = config;
			return this;
		}
	}

	private readonly CK3CharacterBuilder builder = new();

	[Fact]
	public void AllLinksToCharacterAreRemovedWhenCharacterIsRemoved() {
		var date = new Date(400, 1, 1);

		var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {Female = true};
		var imperatorMother = new ImperatorToCK3.Imperator.Characters.Character(2) {Female = true};
		var imperatorFather = new ImperatorToCK3.Imperator.Characters.Character(3) {Female = false};
		var imperatorChild = new ImperatorToCK3.Imperator.Characters.Character(4) {Female = false};
		var imperatorSpouse = new ImperatorToCK3.Imperator.Characters.Character(5) {Female = false};

		imperatorCharacter.Mother = imperatorMother;
		imperatorCharacter.Father = imperatorFather;
		imperatorCharacter.Children.Add(imperatorChild.Id, imperatorChild);
		imperatorCharacter.Spouses.Add(imperatorSpouse.Id, imperatorSpouse);

		var characters = new CharacterCollection();
		var character = builder
			.WithImperatorCharacter(imperatorCharacter)
			.WithCharacterCollection(characters)
			.Build();
		characters.Add(character);
		var mother = builder
			.WithImperatorCharacter(imperatorMother)
			.WithCharacterCollection(characters)
			.Build();
		characters.Add(mother);
		var father = builder
			.WithImperatorCharacter(imperatorFather)
			.WithCharacterCollection(characters)
			.Build();
		characters.Add(father);
		var child = builder
			.WithImperatorCharacter(imperatorChild)
			.WithCharacterCollection(characters)
			.Build();
		characters.Add(child);
		var spouse = builder
			.WithImperatorCharacter(imperatorSpouse)
			.WithCharacterCollection(characters)
			.Build();
		characters.Add(spouse);

		character.Mother = mother;
		character.Father = father;
		child.Mother = character;
		spouse.AddSpouse(date, character);

		Assert.NotNull(character.Mother);
		Assert.NotNull(character.Father);
		character.Children.Select(c => c.Id).Should().Equal("imperator4");
		
		mother.Children.Should().Equal(character);
		father.Children.Should().Equal(character);
		Assert.Equal(character, child.Mother);
		spouse.GetSpouseIds(date).Should().Equal(character.Id);

		characters.Remove(character.Id);

		mother.Children.Should().BeEmpty();
		father.Children.Should().BeEmpty();
		Assert.Null(child.Mother);
		spouse.GetSpouseIds(date).Should().BeEmpty();
	}

	[Fact]
	public void TraitsCanBeInitializedFromImperator() {
		var definedCK3Traits = new IdObjectCollection<string, Trait> {
			new Trait("powerful"),
			new Trait("craven")
		};
		var irToCK3TraitDict = new Dictionary<string, string> {
			{"strong", "powerful"},
			{"craven", "craven"}
		};
		var traitMapper = new TraitMapperTests.TestTraitMapper(irToCK3TraitDict, definedCK3Traits);

		var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
			Traits = new List<string> { "strong", "humble", "craven" }
		};
		var character = builder
			.WithImperatorCharacter(imperatorCharacter)
			.WithTraitMapper(traitMapper)
			.Build();

		var traits = character.History.GetFieldValueAsCollection("traits", new Date());
		Assert.NotNull(traits);
		traits.Should().BeEquivalentTo(new[] { "craven", "powerful" });
	}

	[Fact]
	public void ReligionCanBeInitializedFromImperator() {
		var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
			Religion = "chalcedonian"
		};

		var titles = new Title.LandedTitles();
		var ck3Religions = new ReligionCollection(titles);
		ck3Religions.LoadReligions(CK3ModFS, new ColorFactory());

		var mapReader = new BufferedReader(
			"link = { ir=chalcedonian ck3=orthodox }"
		);
		var religionMapper = new ReligionMapper(mapReader, ck3Religions, IRRegionMapper, new CK3RegionMapper());

		var character = builder
			.WithImperatorCharacter(imperatorCharacter)
			.WithReligionMapper(religionMapper)
			.Build();
		Assert.Equal("orthodox", character.GetFaithId(ConversionDate));
	}

	[Fact]
	public void CultureCanBeInitializedFromImperator() {
		var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
			Culture = "macedonian"
		};

		var mapReader = new BufferedReader(
			"link = { ir=macedonian ck3=greek }"
		);
		var cultureMapper = new CultureMapper(mapReader, IRRegionMapper, new CK3RegionMapper(), cultures);

		var character = builder
			.WithImperatorCharacter(imperatorCharacter)
			.WithCultureMapper(cultureMapper)
			.Build();
		Assert.Equal("greek", character.GetCultureId(ConversionDate));
	}

	[Fact]
	public void GoldCanBeConverterFromImperator() {
		var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
			Wealth = 420.69
		};

		var character = builder
			.WithImperatorCharacter(imperatorCharacter)
			.Build();
		Assert.Equal(420.69, character.Gold);
	}

	[Fact]
	public void AttributesCanBeConverterFromImperator() {
		var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
			Attributes = {
				Charisma = 1,
				Martial = 2,
				Zeal = 3,
				Finesse = 9
			}
		};

		var character = builder
			.WithImperatorCharacter(imperatorCharacter)
			.Build();
		var date = new Date(1, 1, 1);
		Assert.Equal(1, character.History.GetFieldValue("diplomacy", date));
		Assert.Equal(2, character.History.GetFieldValue("martial", date));
		Assert.Equal(3, character.History.GetFieldValue("learning", date));
		Assert.Equal(9, character.History.GetFieldValue("stewardship", date));
		Assert.Equal(5, character.History.GetFieldValue("intrigue", date)); // (charisma+finesse)/2
	}

	[Fact]
	public void ImperatorCountryOfCharacterIsUsedForCultureConversion() {
		var countryReader = new BufferedReader("tag = MAC");
		var country = ImperatorToCK3.Imperator.Countries.Country.Parse(countryReader, 69);

		var imperatorCharacter1 = new ImperatorToCK3.Imperator.Characters.Character(1) {
			Culture = "greek",
			Country = country
		};
		var imperatorCharacter2 = new ImperatorToCK3.Imperator.Characters.Character(2) {
			Culture = "greek"
		};

		var mapReader = new BufferedReader(
			"link = { ir=greek ck3=macedonian historicalTag=MAC }" +
			"link = { ir=greek ck3=greek }"
		);
		var cultureMapper = new CultureMapper(mapReader, IRRegionMapper, new CK3RegionMapper(), cultures);

		var character1 = builder
			.WithImperatorCharacter(imperatorCharacter1)
			.WithCultureMapper(cultureMapper)
			.Build();
		var character2 = builder
			.WithImperatorCharacter(imperatorCharacter2)
			.WithCultureMapper(cultureMapper)
			.Build();

		Assert.Equal("macedonian", character1.GetCultureId(ConversionDate));
		Assert.Equal("greek", character2.GetCultureId(ConversionDate));
	}

	[Fact]
	public void NicknameCanBeInitializedFromImperator() {
		var conversionDate = new Date(200, 1, 1);
		
		var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
			Nickname = "the_goose",
			DeathDate = conversionDate.ChangeByYears(-100)
		};

		var mapReader = new BufferedReader(
			"link = { ir=the_goose ck3=nick_the_goose }"
		);

		var character = builder
			.WithImperatorCharacter(imperatorCharacter)
			.WithNicknameMapper(new NicknameMapper(mapReader))
			.Build();
		Assert.Equal("nick_the_goose", character.GetNickname(conversionDate));
	}

	[Fact]
	public void DeathReasonCanBeInitializedFromImperator() {
		var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
			DeathReason = "shat_to_death",
			DeathDate = new Date(1, 1, 1)
		};

		var mapReader = new BufferedReader(
			"link = { ir=shat_to_death ck3=diarrhea }"
		);

		var character = builder
			.WithImperatorCharacter(imperatorCharacter)
			.WithDeathReasonMapper(new DeathReasonMapper(mapReader))
			.Build();
		Assert.Equal("diarrhea", character.DeathReason);
	}

	[Fact]
	public void NameCanBeInitializedFromImperator() {
		var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
			Name = "alexandros"
		};

		var locDB = new LocDB("english");
		var nameLocBlock = locDB.AddLocBlock("alexandros");
		nameLocBlock["english"] = "Alexandros";

		var character = builder
			.WithImperatorCharacter(imperatorCharacter)
			.WithLocDB(locDB)
			.Build();
		Assert.Equal("alexandros", character.GetName(ConversionDate));
		Assert.Equal("Alexandros", character.Localizations["alexandros"]["english"]);
	}

	[Fact]
	public void AgeSexReturnsCorrectString() {
		GenesDB genesDB = new();
		var conversionDate = new Date(100, 1, 1, AUC: true);
		var reader1 = new BufferedReader(
			"= { birth_date=44.1.1 female=yes }" // age: 56
		);
		var reader2 = new BufferedReader(
			"= { birth_date=44.1.1 }" // age: 56
		);
		var reader3 = new BufferedReader(
			"= { birth_date=92.1.1 female=yes }" // age: 8
		);
		var reader4 = new BufferedReader(
			"= { birth_date=92.1.1 }" // age: 8
		);
		var impCharacter1 = ImperatorToCK3.Imperator.Characters.Character.Parse(reader1, "42", genesDB);
		var impCharacter2 = ImperatorToCK3.Imperator.Characters.Character.Parse(reader2, "43", genesDB);
		var impCharacter3 = ImperatorToCK3.Imperator.Characters.Character.Parse(reader3, "44", genesDB);
		var impCharacter4 = ImperatorToCK3.Imperator.Characters.Character.Parse(reader4, "45", genesDB);
		var character1 = builder
			.WithImperatorCharacter(impCharacter1)
			.Build();
		var character2 = builder
			.WithImperatorCharacter(impCharacter2)
			.Build();
		var character3 = builder
			.WithImperatorCharacter(impCharacter3)
			.Build();
		var character4 = builder
			.WithImperatorCharacter(impCharacter4)
			.Build();

		Assert.Equal("female", character1.GetAgeSex(conversionDate));
		Assert.Equal("male", character2.GetAgeSex(conversionDate));
		Assert.Equal("girl", character3.GetAgeSex(conversionDate));
		Assert.Equal("boy", character4.GetAgeSex(conversionDate));
	}

	[Fact]
	public void UnneededCharactersArePurged() {
		// dead and unlanded from Imperator
		var impCharacterReader = new BufferedReader("death_date = 1.1.1");
		var unlandedFromImperator = builder
			.WithImperatorCharacter(ImperatorToCK3.Imperator.Characters.Character.Parse(impCharacterReader, "1", null))
			.Build();

		var ck3Characters = new CharacterCollection {unlandedFromImperator};

		// dead and unlanded from CK3
		var unlandedFromCK3 = new Character("bob", "Bob", birthDate: new Date("50.1.1"), ck3Characters);
		ck3Characters.Add(unlandedFromCK3);

		var titles = new Title.LandedTitles();
		ck3Characters.PurgeUnneededCharacters(titles, new DynastyCollection(), new HouseCollection(), ConversionDate);

		Assert.Empty(ck3Characters);
	}

	[Fact]
	public void DeadLandlessCharactersArePurgedIfChildless() {
		var titles = new Title.LandedTitles();

		var irFamily = new Family(1);
		var irFamilies = new FamilyCollection { irFamily };
		var irCharacters = new ImperatorToCK3.Imperator.Characters.CharacterCollection();

		var charactersReader = new BufferedReader("""
			1 = { death_date=450.1.1 family=1 father=2 }
			2 = { death_date=400.1.1 family=1 }
			3 = { death_date=440.1.1 family=1 father=2 }
			""");

		irCharacters.LoadCharacters(charactersReader);
		irCharacters.LinkFamilies(irFamilies);

		irCharacters[1].Father.Should().BeSameAs(irCharacters[2]);
		irCharacters[2].Father.Should().BeNull();
		irCharacters[3].Father.Should().BeSameAs(irCharacters[2]);
		
		var ck3Characters = new CharacterCollection();

		// dead but won't be purged because he's landed
		var landedCharacter = builder
			.WithImperatorCharacter(irCharacters[1])
			.WithCharacterCollection(ck3Characters)
			.Build();
		ck3Characters.Add(landedCharacter);
		var kingdom = titles.Add("k_dead_georgia_boys");
		kingdom.SetHolder(landedCharacter, new Date("400.1.1"));
		Assert.Equal("imperator1", kingdom.GetHolderId(new Date("400.1.1")));
		Assert.Collection(kingdom.GetAllHolderIds(),
			id => Assert.Equal("imperator1", id));

		// dead but won't be purged because he belongs to a dynasty of a landed character
		// and has a child
		var fatherOfLandedCharacter = builder
			.WithImperatorCharacter(irCharacters[2])
			.WithCharacterCollection(ck3Characters)
			.Build();
		ck3Characters.Add(fatherOfLandedCharacter);

		// another dead relative, will be purged because he's landless and childless
		var childlessRelative = builder
			.WithImperatorCharacter(irCharacters[3])
			.WithCharacterCollection(ck3Characters)
			.Build();
		ck3Characters.Add(childlessRelative);

		landedCharacter.Father = fatherOfLandedCharacter;
		childlessRelative.Father = fatherOfLandedCharacter;

		var dynasty = new ImperatorToCK3.CK3.Dynasties.Dynasty(irFamily, irCharacters, new CulturesDB(), CultureMapper, new LocDB("english"), ConversionDate);
		var dynasties = new DynastyCollection { dynasty };
		Assert.Equal(dynasty.Id, landedCharacter.GetDynastyId(ConversionDate));
		Assert.Equal(dynasty.Id, fatherOfLandedCharacter.GetDynastyId(ConversionDate));
		Assert.Equal(dynasty.Id, childlessRelative.GetDynastyId(ConversionDate));

		ck3Characters.PurgeUnneededCharacters(titles, dynasties, new HouseCollection(), ConversionDate);

		ck3Characters.Should().BeEquivalentTo(new[] {
			landedCharacter,
			fatherOfLandedCharacter
		});
	}
}