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
	public class CharacterCollection : IdObjectCollection<string, Character> {
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
			LinkSpouses();
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
			this[key].BreakAllLinks();
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

		private void LinkSpouses() {
			var spouseCounter = 0;
			foreach (var ck3Character in this) {
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
					ck3Character.Spouses.TryAdd(ck3SpouseCharacter);
					ck3SpouseCharacter.Spouses.TryAdd(ck3Character);
					++spouseCounter;
				}
			}
			Logger.Info($"{spouseCounter} spouses linked in CK3.");
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
