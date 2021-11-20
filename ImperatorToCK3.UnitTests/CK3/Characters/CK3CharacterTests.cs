using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.Trait;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Characters {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class CK3CharacterTests {
		private class CK3CharacterBuilder {
			private ImperatorToCK3.Imperator.Characters.Character imperatorCharacter = new(0);
			private ReligionMapper religionMapper = new();
			private CultureMapper cultureMapper = new();
			private TraitMapper traitMapper = new("TestFiles/configurables/trait_map.txt");
			private NicknameMapper nicknameMapper = new("TestFiles/configurables/nickname_map.txt");
			private LocalizationMapper localizationMapper = new();
			private ProvinceMapper provinceMapper = new();
			private DeathReasonMapper deathReasonMapper = new();

			public Character Build() {
				var character = new Character(
					imperatorCharacter,
					religionMapper,
					cultureMapper,
					traitMapper,
					nicknameMapper,
					localizationMapper,
					provinceMapper,
					deathReasonMapper,
					new Date(867, 1, 1),
					new Date(867, 1, 1)
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
			public CK3CharacterBuilder WithLocalizationMapper(LocalizationMapper localizationMapper) {
				this.localizationMapper = localizationMapper;
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
		}

		private readonly CK3CharacterBuilder builder = new();

		[Fact]
		public void AllLinksCanBeRemoved() {
			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1);
			var imperatorMother = new ImperatorToCK3.Imperator.Characters.Character(2);
			var imperatorFather = new ImperatorToCK3.Imperator.Characters.Character(3);
			var imperatorChild = new ImperatorToCK3.Imperator.Characters.Character(4);
			var imperatorSpouse = new ImperatorToCK3.Imperator.Characters.Character(5);

			imperatorCharacter.Mother = new(imperatorMother.Id, imperatorMother);
			imperatorCharacter.Father = new(imperatorFather.Id, imperatorFather);
			imperatorCharacter.Children.Add(imperatorChild.Id, imperatorChild);
			imperatorCharacter.Spouses.Add(imperatorSpouse.Id, imperatorSpouse);

			var character = builder
				.WithImperatorCharacter(imperatorCharacter)
				.Build();
			var mother = builder
				.WithImperatorCharacter(imperatorMother)
				.Build();
			var father = builder
				.WithImperatorCharacter(imperatorFather)
				.Build();
			var child = builder
				.WithImperatorCharacter(imperatorChild)
				.Build();
			var spouse = builder
				.WithImperatorCharacter(imperatorSpouse)
				.Build();

			character.Mother = mother;
			character.Father = father;
			character.Children.Add(child.Id, child);
			character.Spouses.Add(spouse.Id, spouse);

			Assert.NotNull(character.Mother);
			Assert.NotNull(character.Father);
			Assert.NotNull(character.Children["imperator4"]);
			Assert.NotNull(character.Spouses["imperator5"]);

			character.BreakAllLinks();

			Assert.Null(character.Mother);
			Assert.Null(character.Father);
			Assert.Empty(character.Children);
			Assert.Empty(character.Spouses);
		}
		[Fact]
		public void BreakAllLinksWarnsWhenSpouseIsNull() {
			var output = new StringWriter();
			Console.SetOut(output);

			var character = builder.Build();
			character.Spouses.Add("spouseId", null);
			character.BreakAllLinks();
			Assert.Contains("[WARN] Spouse spouseId of imperator0 is null!", output.ToString());
		}
		[Fact]
		public void BreakAllLinksWarnsWhenChildIsNull() {
			var output = new StringWriter();
			Console.SetOut(output);

			var male = builder.Build();
			male.Children.Add("childId", null);
			male.BreakAllLinks();
			Assert.Contains("[WARN] Child childId of imperator0 is null!", output.ToString());
			output.Flush();

			var impFemaleReader = new BufferedReader("female = yes");
			var impFemaleCharacter = ImperatorToCK3.Imperator.Characters.Character.Parse(impFemaleReader, "1", null);
			var female = builder.WithImperatorCharacter(impFemaleCharacter).Build();
			female.Children.Add("child2Id", null);
			female.BreakAllLinks();
			Assert.Contains("[WARN] Child child2Id of imperator1 is null!", output.ToString());
		}

		[Fact]
		public void TraitsCanBeInitializedFromImperator() {
			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
				Traits = new() { "strong", "humble", "craven" }
			};
			var traitMapReader = new BufferedReader(
				"link = { imp = strong ck3 = powerful } link = { imp = craven ck3 = craven }"
			);
			var traitMapper = new TraitMapper(traitMapReader);

			var character = builder
				.WithImperatorCharacter(imperatorCharacter)
				.WithTraitMapper(traitMapper)
				.Build();

			Assert.Collection(character.Traits,
				item => Assert.Equal("craven", item),
				item => Assert.Equal("powerful", item)
			);
		}

		[Fact]
		public void ReligionCanBeInitializedFromImperator() {
			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
				Religion = "chalcedonian"
			};

			var mapReader = new BufferedReader(
				"link = { imp=chalcedonian ck3=orthodox }"
			);
			var religionMapper = new ReligionMapper(mapReader);
			religionMapper.LoadRegionMappers(new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper(), new ImperatorToCK3.Mappers.Region.CK3RegionMapper());

			var character = builder
				.WithImperatorCharacter(imperatorCharacter)
				.WithReligionMapper(religionMapper)
				.Build();
			Assert.Equal("orthodox", character.Religion);
		}

		[Fact]
		public void CultureCanBeInitializedFromImperator() {
			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
				Culture = "macedonian"
			};

			var mapReader = new BufferedReader(
				"link = { imp=macedonian ck3=greek }"
			);
			var cultureMapper = new CultureMapper(mapReader);
			cultureMapper.LoadRegionMappers(
				new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper(),
				new ImperatorToCK3.Mappers.Region.CK3RegionMapper()
			);

			var character = builder
				.WithImperatorCharacter(imperatorCharacter)
				.WithCultureMapper(cultureMapper)
				.Build();
			Assert.Equal("greek", character.Culture);
		}

		[Fact]
		public void ImperatorCountryOfCharacterIsUsedForCultureConversion() {
			var countryReader = new BufferedReader("tag = RAN");
			var country = ImperatorToCK3.Imperator.Countries.Country.Parse(countryReader, 69);
			var ck3Title = new ImperatorToCK3.CK3.Titles.Title("d_rankless");
			country.CK3Title = ck3Title;

			var imperatorCharacter1 = new ImperatorToCK3.Imperator.Characters.Character(1) {
				Culture = "greek",
				Country = country
			};
			var imperatorCharacter2 = new ImperatorToCK3.Imperator.Characters.Character(2) {
				Culture = "greek"
			};

			var mapReader = new BufferedReader(
				"link = { imp=greek ck3=macedonian owner=d_rankless }" +
				"link = { imp=greek ck3=greek }"
			);
			var cultureMapper = new CultureMapper(mapReader);
			cultureMapper.LoadRegionMappers(new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper(), new ImperatorToCK3.Mappers.Region.CK3RegionMapper());

			var character1 = builder
				.WithImperatorCharacter(imperatorCharacter1)
				.WithCultureMapper(cultureMapper)
				.Build();
			var character2 = builder
				.WithImperatorCharacter(imperatorCharacter2)
				.WithCultureMapper(cultureMapper)
				.Build();

			Assert.Equal("macedonian", character1.Culture);
			Assert.Equal("greek", character2.Culture);
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
			var nameLocBlock = new LocBlock() { english = "Alexandros" };
			nameLocBlock.FillMissingLocsWithEnglish();

			var localizationMapper = new LocalizationMapper();
			localizationMapper.AddLocalization("alexandros", nameLocBlock);

			var character = builder
				.WithImperatorCharacter(imperatorCharacter)
				.WithLocalizationMapper(localizationMapper)
				.Build();
			Assert.Equal("alexandros", character.Name);
			Assert.Equal("Alexandros", character.Localizations["alexandros"].english);
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
	}
}
