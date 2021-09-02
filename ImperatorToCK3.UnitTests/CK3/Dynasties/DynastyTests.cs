using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Trait;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Imperator.Families;
using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Dynasties {
	public class DynastyTests {
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
		public void IdAndNameAreProperlyConverted() {
			var reader = new BufferedReader(string.Empty);
			var family = Family.Parse(reader, 45);

			var locMapper = new LocalizationMapper();
			var dynasty = new Dynasty(family, locMapper);

			Assert.Equal("dynn_IMPTOCK3_45", dynasty.ID);
			Assert.Equal("dynn_IMPTOCK3_45", dynasty.Name);
		}
		[Fact]
		public void CultureIsBasedOnFirstImperatorMember() {
			var reader = new BufferedReader("member = { 21 22 23 }");
			var family = Family.Parse(reader, 45);
			var member1 = new ImperatorToCK3.Imperator.Characters.Character(21) {
				Culture = "roman"
			};
			family.LinkMember(member1);
			var member2 = new ImperatorToCK3.Imperator.Characters.Character(22) {
				Culture = "akan"
			};
			family.LinkMember(member2);
			var member3 = new ImperatorToCK3.Imperator.Characters.Character(23) {
				Culture = "parthian"
			};
			family.LinkMember(member3);

			var cultureMapper = new CultureMapper(
				new BufferedReader(
					"link={imp=roman ck3=not_gypsy} link={imp=akan ck3=akan} link={imp=parthian ck3=parthian}"
				)
			);
			var locMapper = new LocalizationMapper();
			var ck3Member1 = new CK3CharacterBuilder()
				.WithCultureMapper(cultureMapper)
				.WithImperatorCharacter(member1)
				.Build();
			var ck3Member2 = new CK3CharacterBuilder()
				.WithCultureMapper(cultureMapper)
				.WithImperatorCharacter(member2)
				.Build();
			var ck3Member3 = new CK3CharacterBuilder()
				.WithCultureMapper(cultureMapper)
				.WithImperatorCharacter(member3)
				.Build();
			var dynasty = new Dynasty(family, locMapper);

			Assert.Equal("not_gypsy", dynasty.Culture);
		}
	}
}
