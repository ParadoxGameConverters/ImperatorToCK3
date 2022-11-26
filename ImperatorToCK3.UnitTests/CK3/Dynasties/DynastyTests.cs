﻿using commonItems;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Characters;
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
using Xunit;
using Character = ImperatorToCK3.CK3.Characters.Character;

// ReSharper disable StringLiteralTypo

namespace ImperatorToCK3.UnitTests.CK3.Dynasties; 

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class DynastyTests {
	private const string ImperatorRoot = "TestFiles/Imperator/root";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, new Mod[] { });
	private static readonly AreaCollection areas = new();
	private static readonly ImperatorRegionMapper irRegionMapper = new(irModFS, areas);
	private class CK3CharacterBuilder {
		private const string CK3Path = "TestFiles/CK3";
		private const string CK3Root = "TestFiles/CK3/root";

		private Configuration config = new() {
			CK3BookmarkDate = "867.1.1",
			CK3Path = CK3Path
		};

		private static readonly ModFilesystem ck3ModFS = new(CK3Root, new Mod[] { });

		private ImperatorToCK3.Imperator.Characters.Character imperatorCharacter = new(0);
		private ReligionMapper religionMapper = new(new ReligionCollection(new Title.LandedTitles()), irRegionMapper, new CK3RegionMapper());
		private CultureMapper cultureMapper = new(irRegionMapper, new CK3RegionMapper());
		private TraitMapper traitMapper = new("TestFiles/configurables/trait_map.txt", ck3ModFS);
		private NicknameMapper nicknameMapper = new("TestFiles/configurables/nickname_map.txt");
		private LocDB locDB = new("english", "french", "german", "russian", "simp_chinese", "spanish");
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

	[Fact]
	public void IdAndNameAreProperlyConverted() {
		var characters = new CharacterCollection();
		var reader = new BufferedReader(string.Empty);
		var family = Family.Parse(reader, 45);

		var locMapper = new LocDB("english");
		var dynasty = new Dynasty(family, characters, new CulturesDB(), locMapper);

		Assert.Equal("dynn_IMPTOCK3_45", dynasty.Id);
		Assert.Equal("dynn_IMPTOCK3_45", dynasty.Name);
	}

	[Fact]
	public void LocalizationIsConverted() {
		var characters = new CharacterCollection();
		var reader = new BufferedReader("key = cornelii");
		var family = Family.Parse(reader, 45);

		var locDB = new LocDB("english");
		var dynLoc = locDB.AddLocBlock("cornelii");
		dynLoc["english"] = "Cornelii";
		var dynasty = new Dynasty(family, characters, new CulturesDB(), locDB);

		Assert.Equal("dynn_IMPTOCK3_45", dynasty.Localization.Key);
		Assert.Equal("Cornelii", dynasty.Localization.Value["english"]);
	}

	[Fact]
	public void LocalizationDefaultsToUnlocalizedKey() {
		var characters = new CharacterCollection();
		var reader = new BufferedReader("key = cornelii");
		var family = Family.Parse(reader, 45);

		var locDB = new LocDB("english");
		var dynasty = new Dynasty(family, characters, new CulturesDB(), locDB);

		Assert.Equal("dynn_IMPTOCK3_45", dynasty.Localization.Key);
		Assert.Equal("cornelii", dynasty.Localization.Value["english"]);
	}
	[Fact]
	public void CultureIsBasedOnFirstImperatorMember() {
		var characters = new CharacterCollection();
		var reader = new BufferedReader("member = { 21 22 23 }");
		var family = Family.Parse(reader, 45);
		var member1 = new ImperatorToCK3.Imperator.Characters.Character(21) {
			Culture = "roman"
		};
		characters.Add(member1);
		family.AddMember(member1);
		var member2 = new ImperatorToCK3.Imperator.Characters.Character(22) {
			Culture = "akan"
		};
		characters.Add(member2);
		family.AddMember(member2);
		var member3 = new ImperatorToCK3.Imperator.Characters.Character(23) {
			Culture = "parthian"
		};
		characters.Add(member3);
		family.AddMember(member3);

		var cultureMapper = new CultureMapper(
			new BufferedReader(
				"link={imp=roman ck3=not_gypsy} link={imp=akan ck3=akan} link={imp=parthian ck3=parthian}"
			),
			irRegionMapper,
			new CK3RegionMapper()
		);
		var locDB = new LocDB("english");
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
		var dynasty = new Dynasty(family, characters, new CulturesDB(), locDB);

		Assert.Equal("not_gypsy", dynasty.Culture);
	}
}