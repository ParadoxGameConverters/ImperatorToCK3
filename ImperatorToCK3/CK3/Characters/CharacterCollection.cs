using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.Trait;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Characters {
	public partial class CharacterCollection : IdObjectCollection<string, Character> {
		public CharacterCollection() { }
		public void ImportImperatorCharacters(Imperator.World impWorld,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			TraitMapper traitMapper,
			NicknameMapper nicknameMapper,
			LocDB locDB,
			ProvinceMapper provinceMapper,
			DeathReasonMapper deathReasonMapper,
			Date endDate,
			Configuration config
		) {
			Logger.Info("Importing Imperator Characters...");

			foreach (var character in impWorld.Characters) {
				ImportImperatorCharacter(
					character,
					religionMapper,
					cultureMapper,
					traitMapper,
					nicknameMapper,
					locDB,
					provinceMapper,
					deathReasonMapper,
					endDate,
					config
				);
			}
			Logger.Info($"{Count} total characters recognized.");

			LinkMothersAndFathers();
			LinkSpouses(endDate);
			LinkPrisoners();
		}

		private void ImportImperatorCharacter(
			Imperator.Characters.Character character,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			TraitMapper traitMapper,
			NicknameMapper nicknameMapper,
			LocDB locDB,
			ProvinceMapper provinceMapper,
			DeathReasonMapper deathReasonMapper,
			Date endDate,
			Configuration config
		) {
			// Create a new CK3 character
			var newCharacter = new Character(
				character,
				religionMapper,
				cultureMapper,
				traitMapper,
				nicknameMapper,
				locDB,
				provinceMapper,
				deathReasonMapper,
				endDate,
				config
			);
			character.CK3Character = newCharacter;
			Add(newCharacter);
		}

		public override void Remove(string key) {
			this[key].BreakAllLinks(this);
			base.Remove(key);
		}

		private void LinkMothersAndFathers() {
			var motherCounter = 0;
			var fatherCounter = 0;
			foreach (var ck3Character in this) {
				// make links between Imperator characters
				if (ck3Character.ImperatorCharacter is null) {
					// imperatorRegnal characters do not have ImperatorCharacter
					continue;
				}
				var impMotherCharacter = ck3Character.ImperatorCharacter.Mother;
				if (impMotherCharacter is not null) {
					var ck3MotherCharacter = impMotherCharacter.CK3Character;
					if (ck3MotherCharacter is not null) {
						ck3Character.Mother = ck3MotherCharacter;
						ck3MotherCharacter.Children[ck3Character.Id] = ck3Character;
						++motherCounter;
					} else {
						Logger.Warn($"Imperator mother {impMotherCharacter.Id} has no CK3 character!");
					}
				}

				// make links between Imperator characters
				var impFatherCharacter = ck3Character.ImperatorCharacter.Father;
				if (impFatherCharacter is not null) {
					var ck3FatherCharacter = impFatherCharacter.CK3Character;
					if (ck3FatherCharacter is not null) {
						ck3Character.Father = ck3FatherCharacter;
						ck3FatherCharacter.Children[ck3Character.Id] = ck3Character;
						++fatherCounter;
					} else {
						Logger.Warn($"Imperator father {impFatherCharacter.Id} has no CK3 character!");
					}
				}
			}
			Logger.Info($"{motherCounter} mothers and {fatherCounter} fathers linked in CK3.");
		}

		private void LinkSpouses(Date conversionDate) {
			var spouseCounter = 0;
			foreach (var ck3Character in this) {
				if (ck3Character.Female) {
					continue; // we set spouses for males to avoid doubling marriages
				}
				// make links between Imperator characters
				if (ck3Character.ImperatorCharacter is null) {
					// imperatorRegnal characters do not have ImperatorCharacter
					continue;
				}
				foreach (var impSpouseCharacter in ck3Character.ImperatorCharacter.Spouses.Values) {
					var ck3SpouseCharacter = impSpouseCharacter.CK3Character;
					if (ck3SpouseCharacter is null) {
						Logger.Warn($"Imperator spouse {impSpouseCharacter.Id} has no CK3 character!");
						continue;
					}
					
					// Imperator saves don't seem to store marriage date
					Date estimatedMarriageDate = GetEstimatedMarriageDate(ck3Character.ImperatorCharacter, impSpouseCharacter);

					ck3Character.AddSpouse(estimatedMarriageDate, ck3SpouseCharacter);
					++spouseCounter;
				}
			}
			Logger.Info($"{spouseCounter} spouses linked in CK3.");

			Date GetEstimatedMarriageDate(Imperator.Characters.Character imperatorCharacter, Imperator.Characters.Character imperatorSpouse) {
				Date estimatedMarriageDate; // Imperator saves don't seem to store marriage date
				var birthDateOfCommonChild = GetBirthDateOfFirstCommonChild(imperatorCharacter, imperatorSpouse);
				if (birthDateOfCommonChild is not null) {
					estimatedMarriageDate = new Date(birthDateOfCommonChild);
					estimatedMarriageDate = estimatedMarriageDate.ChangeByMonths(-9); // we assume the child was conceived after marriage
				} else if (imperatorCharacter.DeathDate is not null && imperatorSpouse.DeathDate is not null) {
					if (imperatorCharacter.DeathDate < imperatorSpouse.DeathDate) {
						estimatedMarriageDate = new Date(imperatorCharacter.DeathDate);
					} else {
						estimatedMarriageDate = new Date(imperatorSpouse.DeathDate);
					}
					estimatedMarriageDate = estimatedMarriageDate.ChangeByDays(-1); // death is not a good moment to marry
				} else if (imperatorCharacter.DeathDate is not null) {
					estimatedMarriageDate = new Date(imperatorCharacter.DeathDate).ChangeByDays(-1);
				} else if (imperatorSpouse.DeathDate is not null) {
					estimatedMarriageDate = new Date(imperatorSpouse.DeathDate).ChangeByDays(-1);
				} else {
					estimatedMarriageDate = new Date(conversionDate);
				}

				return estimatedMarriageDate;
			}
			Date? GetBirthDateOfFirstCommonChild(Imperator.Characters.Character father, Imperator.Characters.Character mother) {
				var childrenOfFather = father.Children.Values.ToHashSet();
				var childrenOfMother = mother.Children.Values.ToHashSet();
				var commonChildren = childrenOfFather.Intersect(childrenOfMother).OrderBy(child=>child.BirthDate).ToList();

				Date? dateToReturn = commonChildren.Any() ? commonChildren.FirstOrDefault()?.BirthDate : null;
				if (dateToReturn is not null) {
					return dateToReturn;
				}

				var unborns = mother.Unborns.Where(u => u.FatherId == father.Id).OrderBy(u=>u.BirthDate).ToList();
				dateToReturn = unborns.FirstOrDefault()?.BirthDate;

				return dateToReturn;
			}
		}

		private void LinkPrisoners() {
			var prisonerCount = this.Count(character => character.LinkJailor(this));
			Logger.Info($"{prisonerCount} prisoners linked with jailors in CK3.");
		}

		public void PurgeUnneededCharacters(Title.LandedTitles titles) {
			Logger.Info("Purging unneeded characters...");
			var landedCharacterIds = titles.GetAllHolderIds();
			var landedCharacters = this.Where(character => landedCharacterIds.Contains(character.Id));
			var dynastyIdsOfLandedCharacters = landedCharacters.Select(character => character.DynastyId).Distinct().ToHashSet();

			var farewellIds = new List<string>();

			var charactersToCheck = this.Except(landedCharacters);
			foreach (var character in charactersToCheck) {
				var id = character.Id;

				if (character.FromImperator && !character.Dead) {
					continue;
				}

				if (dynastyIdsOfLandedCharacters.Contains(character.DynastyId)) {
					continue;
				}

				farewellIds.Add(id);
			}

			foreach (var characterId in farewellIds) {
				Remove(characterId);
			}
			Logger.Info($"Purged {farewellIds.Count} unneeded characters.");
		}

		public void RemoveEmployerIdFromLandedCharacters(Title.LandedTitles titles, Date conversionDate) {
			Logger.Info("Removing employer id from landed characters...");
			var landedCharacterIds = titles.GetHolderIds(conversionDate);
			foreach (var character in this.Where(character => landedCharacterIds.Contains(character.Id))) {
				character.EmployerId = null;
			}
		}
	}
}
