using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImperatorToCK3.CK3.Characters;
using Xunit;
using ImperatorToCK3.Imperator;

namespace ImperatorToCK3.UnitTests.CK3.Characters {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class CK3CharacterTests {
		private const string traitMapPath = "TestFiles/configurables/trait_map.txt";
		private const string nicknameMapPath = "TestFiles/configurables/nickname_map.txt";
		[Fact] public void AllLinksCanBeRemoved() {
			var imperatorCharacter = new ImperatorToCK3.Imperator.Characters.Character(1);
			var imperatorMother = new ImperatorToCK3.Imperator.Characters.Character(2);
			var imperatorFather = new ImperatorToCK3.Imperator.Characters.Character(3);
			var imperatorChild = new ImperatorToCK3.Imperator.Characters.Character(4);
			var imperatorSpouse = new ImperatorToCK3.Imperator.Characters.Character(5);

			imperatorCharacter.Mother = new(imperatorMother.ID, imperatorMother);
			imperatorCharacter.Father = new(imperatorFather.ID, imperatorFather);
			imperatorCharacter.Children.Add(imperatorChild.ID, imperatorChild);
			imperatorCharacter.Spouses.Add(imperatorSpouse.ID, imperatorSpouse);

			var character = new Character();
			character.InitializeFromImperator(imperatorCharacter,
				new ImperatorToCK3.Mappers.Religion.ReligionMapper(),
				new ImperatorToCK3.Mappers.Culture.CultureMapper(),
				new ImperatorToCK3.Mappers.Trait.TraitMapper(traitMapPath),
				new ImperatorToCK3.Mappers.Nickname.NicknameMapper(nicknameMapPath),
				new ImperatorToCK3.Mappers.Localization.LocalizationMapper(),
				new ImperatorToCK3.Mappers.Province.ProvinceMapper(),
				new ImperatorToCK3.Mappers.DeathReason.DeathReasonMapper(),
				ConvertBirthAndDeathDates: true);
			var mother = new Character();
			mother.InitializeFromImperator(imperatorMother,
				new ImperatorToCK3.Mappers.Religion.ReligionMapper(),
				new ImperatorToCK3.Mappers.Culture.CultureMapper(),
				new ImperatorToCK3.Mappers.Trait.TraitMapper(traitMapPath),
				new ImperatorToCK3.Mappers.Nickname.NicknameMapper(nicknameMapPath),
				new ImperatorToCK3.Mappers.Localization.LocalizationMapper(),
				new ImperatorToCK3.Mappers.Province.ProvinceMapper(),
				new ImperatorToCK3.Mappers.DeathReason.DeathReasonMapper(),
				ConvertBirthAndDeathDates: true);
			var father = new Character();
			father.InitializeFromImperator(imperatorFather,
				new ImperatorToCK3.Mappers.Religion.ReligionMapper(),
				new ImperatorToCK3.Mappers.Culture.CultureMapper(),
				new ImperatorToCK3.Mappers.Trait.TraitMapper(traitMapPath),
				new ImperatorToCK3.Mappers.Nickname.NicknameMapper(nicknameMapPath),
				new ImperatorToCK3.Mappers.Localization.LocalizationMapper(),
				new ImperatorToCK3.Mappers.Province.ProvinceMapper(),
				new ImperatorToCK3.Mappers.DeathReason.DeathReasonMapper(),
				ConvertBirthAndDeathDates: true);
			var child = new Character();
			child.InitializeFromImperator(imperatorChild,
				new ImperatorToCK3.Mappers.Religion.ReligionMapper(),
				new ImperatorToCK3.Mappers.Culture.CultureMapper(),
				new ImperatorToCK3.Mappers.Trait.TraitMapper(traitMapPath),
				new ImperatorToCK3.Mappers.Nickname.NicknameMapper(nicknameMapPath),
				new ImperatorToCK3.Mappers.Localization.LocalizationMapper(),
				new ImperatorToCK3.Mappers.Province.ProvinceMapper(),
				new ImperatorToCK3.Mappers.DeathReason.DeathReasonMapper(),
				ConvertBirthAndDeathDates: true);
			var spouse = new Character();
			spouse.InitializeFromImperator(imperatorSpouse,
				new ImperatorToCK3.Mappers.Religion.ReligionMapper(),
				new ImperatorToCK3.Mappers.Culture.CultureMapper(),
				new ImperatorToCK3.Mappers.Trait.TraitMapper(traitMapPath),
				new ImperatorToCK3.Mappers.Nickname.NicknameMapper(nicknameMapPath),
				new ImperatorToCK3.Mappers.Localization.LocalizationMapper(),
				new ImperatorToCK3.Mappers.Province.ProvinceMapper(),
				new ImperatorToCK3.Mappers.DeathReason.DeathReasonMapper(),
				ConvertBirthAndDeathDates: true);

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
	}
}
