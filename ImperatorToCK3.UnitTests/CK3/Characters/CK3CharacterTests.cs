using System;
using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Mappers.Religion;
using Xunit;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Trait;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.DeathReason;
using System.IO;

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
			private bool convertBirthAndDeathDates = true;

			public Character Build() {
				var character = new Character();
				character.InitializeFromImperator(
					imperatorCharacter,
					religionMapper,
					cultureMapper,
					traitMapper,
					nicknameMapper,
					localizationMapper,
					provinceMapper,
					deathReasonMapper,
					convertBirthAndDeathDates
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

		[Fact]
		public void AllLinksCanBeRemoved() {
			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1);
			var imperatorMother = new ImperatorToCK3.Imperator.Characters.Character(2);
			var imperatorFather = new ImperatorToCK3.Imperator.Characters.Character(3);
			var imperatorChild = new ImperatorToCK3.Imperator.Characters.Character(4);
			var imperatorSpouse = new ImperatorToCK3.Imperator.Characters.Character(5);

			imperatorCharacter.Mother = new(imperatorMother.ID, imperatorMother);
			imperatorCharacter.Father = new(imperatorFather.ID, imperatorFather);
			imperatorCharacter.Children.Add(imperatorChild.ID, imperatorChild);
			imperatorCharacter.Spouses.Add(imperatorSpouse.ID, imperatorSpouse);

			var character = new CK3CharacterBuilder()
				.WithImperatorCharacter(imperatorCharacter)
				.Build();
			var mother = new CK3CharacterBuilder()
				.WithImperatorCharacter(imperatorMother)
				.Build();
			var father = new CK3CharacterBuilder()
				.WithImperatorCharacter(imperatorFather)
				.Build();
			var child = new CK3CharacterBuilder()
				.WithImperatorCharacter(imperatorChild)
				.Build();
			var spouse = new CK3CharacterBuilder()
				.WithImperatorCharacter(imperatorSpouse)
				.Build();

			character.Mother = mother;
			character.Father = father;
			character.Children.Add(child.ID, child);
			character.Spouses.Add(spouse.ID, spouse);

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
		public void TraitsCanBeInitializedFromImperator() {
			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
				Traits = new() { "strong", "humble", "craven" }
			};
			var traitMapReader = new BufferedReader(
				"link = { imp = strong ck3 = powerful } link = { imp = craven ck3 = craven }"
			);
			var traitMapper = new TraitMapper(traitMapReader);

			var character = new CK3CharacterBuilder()
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

			var character = new CK3CharacterBuilder()
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
			cultureMapper.LoadRegionMappers(new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper(), new ImperatorToCK3.Mappers.Region.CK3RegionMapper());

			var character = new CK3CharacterBuilder()
				.WithImperatorCharacter(imperatorCharacter)
				.WithCultureMapper(cultureMapper)
				.Build();
			Assert.Equal("greek", character.Culture);
		}

		[Fact]
		public void NicknameCanBeInitializedFromImperator() {
			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1) {
				Nickname = "the_goose"
			};

			var mapReader = new BufferedReader(
				"link = { imp=the_goose ck3=nick_the_goose }"
			);

			var character = new CK3CharacterBuilder()
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

			var character = new CK3CharacterBuilder()
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

			var character = new CK3CharacterBuilder()
				.WithImperatorCharacter(imperatorCharacter)
				.WithLocalizationMapper(localizationMapper)
				.Build();
			Assert.Equal("alexandros", character.Name);
			Assert.Equal("Alexandros", character.Localizations["alexandros"].english);
		}

		[Fact]
		public void LinkingParentWithWrongIdIsLogged() {
			var character = new CK3CharacterBuilder().Build();
			character.PendingMotherID = "imperator1";
			character.PendingFatherID = "imperator2";

			var mother = new CK3CharacterBuilder()
				.WithImperatorCharacter(new ImperatorToCK3.Imperator.Characters.Character(69))
				.Build();
			var father = new CK3CharacterBuilder()
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
