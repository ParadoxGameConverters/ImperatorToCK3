using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using commonItems.Mods;
using FluentAssertions;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Cultures;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.Trait;
using ImperatorToCK3.UnitTests.Mappers.Trait;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Characters {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class CK3CharacterTests {
		private const string CK3Path = "TestFiles/CK3";
		private const string CK3Root = "TestFiles/CK3/game";
		private static readonly ModFilesystem ck3ModFS = new(CK3Root, new Mod[] { });
		
		public class CK3CharacterBuilder {
			private Configuration config = new() {
				CK3BookmarkDate = "867.1.1",
				CK3Path = CK3Path
			};
			
			private ImperatorToCK3.Imperator.Characters.Character imperatorCharacter = new(0);
			private ReligionMapper religionMapper = new(new ReligionCollection(), new ImperatorRegionMapper(), new CK3RegionMapper());
			private CultureMapper cultureMapper = new(new ImperatorRegionMapper(), new CK3RegionMapper());
			private TraitMapper traitMapper = new("TestFiles/configurables/trait_map.txt", ck3ModFS);
			private NicknameMapper nicknameMapper = new("TestFiles/configurables/nickname_map.txt");
			private LocDB locDB = new("english");
			private ProvinceMapper provinceMapper = new();
			private DeathReasonMapper deathReasonMapper = new();

			public Character Build() {
				var character = new Character(
					imperatorCharacter,
					religionMapper,
					cultureMapper,
					traitMapper,
					nicknameMapper,
					locDB,
					provinceMapper,
					deathReasonMapper,
					new Date(867, 1, 1),
					config
				);
				return character;
			}
			public CK3CharacterBuilder WithImperatorCharacter(ImperatorToCK3.Imperator.Characters.Character imperatorCharacter) {
				this.imperatorCharacter = imperatorCharacter;
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
			public CK3CharacterBuilder WithLocDB(LocDB LocDB) {
				this.locDB = LocDB;
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
		public void AllLinksCanBeRemoved() {
			var date = new Date(400, 1, 1);

			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1);
			var imperatorMother = new ImperatorToCK3.Imperator.Characters.Character(2);
			var imperatorFather = new ImperatorToCK3.Imperator.Characters.Character(3);
			var imperatorChild = new ImperatorToCK3.Imperator.Characters.Character(4);
			var imperatorSpouse = new ImperatorToCK3.Imperator.Characters.Character(5);

			imperatorCharacter.Mother = imperatorMother;
			imperatorCharacter.Father = imperatorFather;
			imperatorCharacter.Children.Add(imperatorChild.Id, imperatorChild);
			imperatorCharacter.Spouses.Add(imperatorSpouse.Id, imperatorSpouse);

			var characters = new CharacterCollection();
			var character = builder
				.WithImperatorCharacter(imperatorCharacter)
				.Build();
			characters.Add(character);
			var mother = builder
				.WithImperatorCharacter(imperatorMother)
				.Build();
			characters.Add(mother);
			var father = builder
				.WithImperatorCharacter(imperatorFather)
				.Build();
			characters.Add(father);
			var child = builder
				.WithImperatorCharacter(imperatorChild)
				.Build();
			characters.Add(child);
			var spouse = builder
				.WithImperatorCharacter(imperatorSpouse)
				.Build();
			characters.Add(spouse);

			character.Mother = mother;
			character.Father = father;
			character.Children.Add(child.Id, child);
			character.AddSpouse(date, spouse);

			Assert.NotNull(character.Mother);
			Assert.NotNull(character.Father);
			Assert.NotNull(character.Children["imperator4"]);
			var spousesAtDate = character.GetSpouseIds(date);
			Assert.NotNull(spousesAtDate);
			Assert.Contains("imperator5", spousesAtDate);

			character.BreakAllLinks(characters);

			Assert.Null(character.Mother);
			Assert.Null(character.Father);
			Assert.Empty(character.Children);
			spousesAtDate = character.GetSpouseIds(date);
			Assert.NotNull(spousesAtDate);
			Assert.Empty(spousesAtDate);
		}
		[Fact]
		public void BreakAllLinksWarnsWhenChildIsNull() {
			var output = new StringWriter();
			Console.SetOut(output);

			var characters = new CharacterCollection();
			var male = builder.Build();
			characters.Add(male);
			male.Children.Add("childId", null);
			male.BreakAllLinks(characters);
			Assert.Contains("[WARN] Child childId of imperator0 is null!", output.ToString());
			output.Flush();

			var impFemaleReader = new BufferedReader("female = yes");
			var impFemaleCharacter = ImperatorToCK3.Imperator.Characters.Character.Parse(impFemaleReader, "1", null);
			var female = builder.WithImperatorCharacter(impFemaleCharacter).Build();
			characters.Add(female);
			female.Children.Add("child2Id", null);
			female.BreakAllLinks(characters);
			Assert.Contains("[WARN] Child child2Id of imperator1 is null!", output.ToString());
		}

		[Fact]
		public void TraitsCanBeInitializedFromImperator() {
			var definedCK3Traits = new IdObjectCollection<string, Trait> {
				new Trait("powerful"),
				new Trait("craven")
			};
			var impToCK3TraitDict = new Dictionary<string, string> {
				{"strong", "powerful"},
				{"craven", "craven"}
			};
			var traitMapper = new TraitMapperTests.TestTraitMapper(impToCK3TraitDict, definedCK3Traits);

			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
				Traits = new() { "strong", "humble", "craven" }
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
			
			var ck3Religions = new ReligionCollection();
			ck3Religions.LoadReligions(ck3ModFS);

			var mapReader = new BufferedReader(
				"link = { imp=chalcedonian ck3=orthodox }"
			);
			var religionMapper = new ReligionMapper(mapReader, ck3Religions, new ImperatorRegionMapper(), new CK3RegionMapper());

			var character = builder
				.WithImperatorCharacter(imperatorCharacter)
				.WithReligionMapper(religionMapper)
				.Build();
			Assert.Equal("orthodox", character.FaithId);
		}

		[Fact]
		public void CultureCanBeInitializedFromImperator() {
			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
				Culture = "macedonian"
			};

			var mapReader = new BufferedReader(
				"link = { imp=macedonian ck3=greek }"
			);
			var cultureMapper = new CultureMapper(mapReader, new ImperatorRegionMapper(), new CK3RegionMapper());

			var character = builder
				.WithImperatorCharacter(imperatorCharacter)
				.WithCultureMapper(cultureMapper)
				.Build();
			Assert.Equal("greek", character.CultureId);
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
					Finesse = 4
				}
			};

			var character = builder
				.WithImperatorCharacter(imperatorCharacter)
				.Build();
			var date = new Date(1, 1, 1);
			Assert.Equal(1, character.History.GetFieldValue("diplomacy", date));
			Assert.Equal(2, character.History.GetFieldValue("martial", date));
			Assert.Equal(3, character.History.GetFieldValue("learning", date));
			Assert.Equal(4, character.History.GetFieldValue("stewardship", date));
			Assert.Equal(4, character.History.GetFieldValue("intrigue", date));
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
				"link = { imp=greek ck3=macedonian tag=MAC }" +
				"link = { imp=greek ck3=greek }"
			);
			var cultureMapper = new CultureMapper(mapReader, new ImperatorRegionMapper(), new CK3RegionMapper());

			var character1 = builder
				.WithImperatorCharacter(imperatorCharacter1)
				.WithCultureMapper(cultureMapper)
				.Build();
			var character2 = builder
				.WithImperatorCharacter(imperatorCharacter2)
				.WithCultureMapper(cultureMapper)
				.Build();

			Assert.Equal("macedonian", character1.CultureId);
			Assert.Equal("greek", character2.CultureId);
		}

		[Fact]
		public void NicknameCanBeInitializedFromImperator() {
			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
				Nickname = "the_goose"
			};

			var mapReader = new BufferedReader(
				"link = { imp=the_goose ck3=nick_the_goose }"
			);

			var character = builder
				.WithImperatorCharacter(imperatorCharacter)
				.WithNicknameMapper(new NicknameMapper(mapReader))
				.Build();
			Assert.Equal("nick_the_goose", character.Nickname);
		}

		[Fact]
		public void DeathReasonCanBeInitializedFromImperator() {
			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
				DeathReason = "shat_to_death"
			};

			var mapReader = new BufferedReader(
				"link = { imp=shat_to_death ck3=diarrhea }"
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
			Assert.Equal("alexandros", character.Name);
			Assert.Equal("Alexandros", character.Localizations["alexandros"]["english"]);
		}

		[Fact]
		public void AgeSexReturnsCorrectString() {
			ImperatorToCK3.Imperator.Genes.GenesDB genesDB = new();
			var reader1 = new BufferedReader(
				"= {\n" +
				"\tage=56\n" +
				"\tfemale=yes\n" +
				"}"
			);
			var reader2 = new BufferedReader(
				"= {\n" +
				"\tage=56\n" +
				"}"
			);
			var reader3 = new BufferedReader(
				"= {\n" +
				"\tage=8\n" +
				"\tfemale=yes\n" +
				"}"
			);
			var reader4 = new BufferedReader(
				"= {\n" +
				"\tage=8\n" +
				"}"
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

			Assert.Equal("female", character1.AgeSex);
			Assert.Equal("male", character2.AgeSex);
			Assert.Equal("girl", character3.AgeSex);
			Assert.Equal("boy", character4.AgeSex);
		}

		[Fact]
		public void LinkingParentWithWrongIdIsLogged() {
			var character = builder.Build();
			character.PendingMotherId = "imperator1";
			character.PendingFatherId = "imperator2";

			var mother = builder
				.WithImperatorCharacter(new ImperatorToCK3.Imperator.Characters.Character(69))
				.Build();
			var father = builder
				.WithImperatorCharacter(new ImperatorToCK3.Imperator.Characters.Character(420))
				.Build();

			var output = new StringWriter();
			Console.SetOut(output);

			character.Mother = mother;
			character.Father = father;

			Assert.Contains("Character imperator0: linking mother imperator69 instead of expected imperator1", output.ToString());
			Assert.Contains("Character imperator0: linking father imperator420 instead of expected imperator2", output.ToString());
		}

		[Fact]
		public void UnneededCharactersArePurged() {
			// dead and unlanded from Imperator
			var impCharacterReader = new BufferedReader("death_date = 1.1.1");
			var imperatorUnlanded = builder
				.WithImperatorCharacter(ImperatorToCK3.Imperator.Characters.Character.Parse(impCharacterReader, "1", null))
				.Build();

			// dead and unlanded from CK3
			var ck3Unlanded = new Character("bob", "Bob", birthDate: new Date("50.1.1"));

			var characters = new CharacterCollection {
				imperatorUnlanded,
				ck3Unlanded
			};

			var titles = new Title.LandedTitles();
			characters.PurgeUnneededCharacters(titles);

			Assert.Empty(characters);
		}

		[Fact]
		public void NeededCharactersAreNotPurged() {
			var titles = new Title.LandedTitles();

			var impFamily = new Family(1);
			var impFamilies = new FamilyCollection { impFamily };
			var impCharacters = new ImperatorToCK3.Imperator.Characters.CharacterCollection();

			var impCharacterReader = new BufferedReader("{ death_date=450.1.1 family=1 }");
			var impCharacter1 = ImperatorToCK3.Imperator.Characters.Character.Parse(impCharacterReader, "1", null);
			impCharacters.Add(impCharacter1);
			impCharacter1.LinkFamily(impFamilies);

			impCharacterReader = new BufferedReader("{ death_date=2.1.1 family=1 }");
			var impCharacter2 = ImperatorToCK3.Imperator.Characters.Character.Parse(impCharacterReader, "2", null);
			impCharacters.Add(impCharacter2);
			impCharacter2.LinkFamily(impFamilies);

			// dead but won't be purged because he's landed
			var landedCharacter = builder
				.WithImperatorCharacter(impCharacter1)
				.Build();
			var kingdom = titles.Add("k_dead_georgia_boys");
			kingdom.SetHolder(landedCharacter, new Date("400.1.1"));
			Assert.Equal("imperator1", kingdom.GetHolderId(new Date("400.1.1")));
			Assert.Collection(kingdom.GetAllHolderIds(),
				id => Assert.Equal("imperator1", id));

			// dead but won't be purged because he belongs to a dynasty of a landed character
			var relativeOfLandedCharacter = builder
				.WithImperatorCharacter(impCharacter2)
				.Build();

			var dynasty = new ImperatorToCK3.CK3.Dynasties.Dynasty(impFamily, impCharacters, new CulturesDB(), new LocDB("english"));
			Assert.Equal(dynasty.Id, landedCharacter.DynastyId);
			Assert.Equal(dynasty.Id, relativeOfLandedCharacter.DynastyId);

			var characters = new CharacterCollection{
				landedCharacter,
				relativeOfLandedCharacter
			};
			characters.PurgeUnneededCharacters(titles);

			Assert.Collection(characters,
				character => Assert.Same(landedCharacter, character),
				character => Assert.Same(relativeOfLandedCharacter, character)
			);
		}
	}
}
