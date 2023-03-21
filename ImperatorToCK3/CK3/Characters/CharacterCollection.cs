using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CK3.Armies;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Armies;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.Trait;
using ImperatorToCK3.Mappers.UnitType;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Characters;

public partial class CharacterCollection : IdObjectCollection<string, Character> {
	public void ImportImperatorCharacters(
		Imperator.World impWorld,
		ReligionMapper religionMapper,
		CultureMapper cultureMapper,
		TraitMapper traitMapper,
		NicknameMapper nicknameMapper,
		LocDB locDB,
		ProvinceMapper provinceMapper,
		DeathReasonMapper deathReasonMapper,
		DNAFactory dnaFactory,
		Date conversionDate,
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
				dnaFactory,
				conversionDate,
				config
			);
		}
		Logger.Info($"{Count} total characters recognized.");

		LinkMothersAndFathers();
		LinkSpouses(conversionDate);
		LinkPrisoners(conversionDate);

		ImportFriendships(impWorld.Characters, conversionDate);
		ImportRivalries(impWorld.Characters, conversionDate);
		Logger.IncrementProgress();
		
		ImportPregnancies(impWorld.Characters, conversionDate);
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
		DNAFactory dnaFactory,
		Date endDate,
		Configuration config
	) {
		// Create a new CK3 character
		var newCharacter = new Character(
			character,
			this,
			religionMapper,
			cultureMapper,
			traitMapper,
			nicknameMapper,
			locDB,
			provinceMapper,
			deathReasonMapper,
			dnaFactory,
			endDate,
			config
		);
		character.CK3Character = newCharacter;
		Add(newCharacter);
	}

	public override void Remove(string key) {
		var character = this[key];

		character.RemoveAllSpouses();
		character.RemoveAllChildren();

		var irCharacter = character.ImperatorCharacter;
		if (irCharacter is not null) {
			irCharacter.CK3Character = null;
		}
		
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
			var irMotherCharacter = ck3Character.ImperatorCharacter.Mother;
			if (irMotherCharacter is not null) {
				var ck3MotherCharacter = irMotherCharacter.CK3Character;
				if (ck3MotherCharacter is not null) {
					ck3Character.Mother = ck3MotherCharacter;
					++motherCounter;
				} else {
					Logger.Warn($"Imperator mother {irMotherCharacter.Id} has no CK3 character!");
				}
			}

			// make links between Imperator characters
			var irFatherCharacter = ck3Character.ImperatorCharacter.Father;
			if (irFatherCharacter is not null) {
				var ck3FatherCharacter = irFatherCharacter.CK3Character;
				if (ck3FatherCharacter is not null) {
					ck3Character.Father = ck3FatherCharacter;
					++fatherCounter;
				} else {
					Logger.Warn($"Imperator father {irFatherCharacter.Id} has no CK3 character!");
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
			// Imperator saves don't seem to store marriage date.

			var marriageDeathDate = GetMarriageDeathDate(imperatorCharacter, imperatorSpouse);
			var birthDateOfCommonChild = GetBirthDateOfFirstCommonChild(imperatorCharacter, imperatorSpouse);
			if (birthDateOfCommonChild is not null) {
				// We assume the child was conceived after marriage.
				var estimatedConceptionDate = birthDateOfCommonChild.ChangeByDays(-280);
				if (marriageDeathDate is not null && marriageDeathDate < estimatedConceptionDate) {
					estimatedConceptionDate = marriageDeathDate.ChangeByDays(-1);
				}
				return estimatedConceptionDate;
			}

			if (marriageDeathDate is not null) {
				return marriageDeathDate.ChangeByDays(-1); // Death is not a good moment to marry.
			}

			return conversionDate;
		}
		Date? GetBirthDateOfFirstCommonChild(Imperator.Characters.Character father, Imperator.Characters.Character mother) {
			var childrenOfFather = father.Children.Values.ToHashSet();
			var childrenOfMother = mother.Children.Values.ToHashSet();
			var commonChildren = childrenOfFather.Intersect(childrenOfMother).OrderBy(child => child.BirthDate).ToList();

			Date? firstChildBirthDate = commonChildren.Count > 0 ? commonChildren.FirstOrDefault()?.BirthDate : null;
			if (firstChildBirthDate is not null) {
				return firstChildBirthDate;
			}

			var unborns = mother.Unborns.Where(u => u.FatherId == father.Id).OrderBy(u => u.BirthDate).ToList();
			return unborns.FirstOrDefault()?.BirthDate;
		}

		Date? GetMarriageDeathDate(Imperator.Characters.Character husband, Imperator.Characters.Character wife) {
			if (husband.DeathDate is not null && wife.DeathDate is not null) {
				return husband.DeathDate < wife.DeathDate ? husband.DeathDate : wife.DeathDate;
			}
			return husband.DeathDate ?? wife.DeathDate;
		}
	}

	private void LinkPrisoners(Date date) {
		var prisonerCount = this.Count(character => character.LinkJailor(date));
		Logger.Info($"{prisonerCount} prisoners linked with jailors in CK3.");
	}

	private static void ImportFriendships(Imperator.Characters.CharacterCollection irCharacters, Date conversionDate) {
		Logger.Info("Importing friendships...");
		foreach (var irCharacter in irCharacters) {
			var ck3Character = irCharacter.CK3Character;
			if (ck3Character is null) {
				Logger.Warn($"Imperator character {irCharacter.Id} has no CK3 character!");
				continue;
			}

			foreach (var irFriendId in irCharacter.FriendIds) {
				var irFriend = irCharacters[irFriendId];
				var ck3Friend = irFriend.CK3Character;
				
				if (ck3Friend is not null) {
					var effectStr = $"{{ set_relation_friend={{ reason=friend_generic_history target=character:{ck3Friend.Id} }} }}";
					ck3Character.History.AddFieldValue(conversionDate, "effects", "effect", effectStr);
				} else {
					Logger.Warn($"Imperator friend {irFriendId} has no CK3 character!");
				}
			}
		}
	}

	private static void ImportRivalries(Imperator.Characters.CharacterCollection irCharacters, Date conversionDate) {
		Logger.Info("Importing rivalries...");
		foreach (var irCharacter in irCharacters) {
			var ck3Character = irCharacter.CK3Character;
			if (ck3Character is null) {
				Logger.Warn($"Imperator character {irCharacter.Id} has no CK3 character!");
				continue;
			}

			foreach (var irRivalId in irCharacter.RivalIds) {
				var irRival = irCharacters[irRivalId];
				var ck3Rival = irRival.CK3Character;
				
				if (ck3Rival is not null) {
					var effectStr = $"{{ set_relation_rival={{ reason=rival_historical target=character:{ck3Rival.Id} }} }}";
					ck3Character.History.AddFieldValue(conversionDate, "effects", "effect", effectStr);
				} else {
					Logger.Warn($"Imperator rival {irRivalId} has no CK3 character!");
				}
			}
		}
	}
	private void ImportPregnancies(Imperator.Characters.CharacterCollection imperatorCharacters, Date conversionDate) {
		Logger.Info("Importing pregnancies...");
		foreach (var female in this.Where(c => c.Female)) {
			var imperatorFemale = female.ImperatorCharacter;
			if (imperatorFemale is null) {
				continue;
			}

			foreach (var unborn in imperatorFemale.Unborns) {
				var conceptionDate = unborn.EstimatedConceptionDate;

				// in CK3 the make_pregnant effect used in character history is executed on game start, so
				// it only makes sense to convert pregnancies that lasted around 3 months or less
				// (longest recorded pregnancy was around 12 months)
				var pregnancyLength = conversionDate.DiffInYears(conceptionDate);
				if (pregnancyLength > 0.25) {
					continue;
				}

				if (!imperatorCharacters.TryGetValue(unborn.FatherId, out var imperatorFather)) {
					continue;
				}

				var ck3Father = imperatorFather.CK3Character;
				if (ck3Father is null) {
					continue;
				}

				female.Pregnancies.Add(new(ck3Father.Id, female.Id, unborn.BirthDate, unborn.IsBastard));
			}
		}

		Logger.IncrementProgress();
	}

	public void PurgeUnneededCharacters(Title.LandedTitles titles, Date date) {
		Logger.Info("Purging unneeded characters...");
		var landedCharacterIds = titles.GetAllHolderIds();
		var landedCharacters = this
			.Where(character => landedCharacterIds.Contains(character.Id))
			.ToList();
		var dynastyIdsOfLandedCharacters = landedCharacters
			.Select(character => character.GetDynastyId(date))
			.Distinct()
			.ToHashSet();

		// Characters that hold or held titles should always be kept.
		var charactersToCheck = this.Except(landedCharacters).ToList();

		var i = 0;
		var farewellCharacters = new List<Character>();
		var parentIdsCache = new HashSet<string>();
		do {
			farewellCharacters.Clear();
			parentIdsCache.Clear();
			++i;
			
			// Build cache of all parent IDs.
			foreach (var character in this) {
				var motherId = character.MotherId;
				if (motherId is not null) {
					parentIdsCache.Add(motherId);
				}

				var fatherId = character.FatherId;
				if (fatherId is not null) {
					parentIdsCache.Add(fatherId);
				}
			}

			// See who can be removed.
			foreach (var character in charactersToCheck) {				
				// Keep alive characters.
				if (character is {FromImperator: true, Dead: false}) {
					continue;
				}

				// Does the character belong to a dynasty that holds or held titles?
				if (dynastyIdsOfLandedCharacters.Contains(character.GetDynastyId(date))) {
					// Is the character dead and childless? Purge.
					if (!parentIdsCache.Contains(character.Id)) {
						farewellCharacters.Add(character);
					}

					continue;
				}

				farewellCharacters.Add(character);
			}

			foreach (var characterToRemove in farewellCharacters) {
				Remove(characterToRemove.Id);
			}

			Logger.Debug($"Purged {farewellCharacters.Count} unneeded characters in iteration {i}.");
			charactersToCheck = charactersToCheck.Except(farewellCharacters).ToList();
		} while(farewellCharacters.Count > 0);
	}

	public void RemoveEmployerIdFromLandedCharacters(Title.LandedTitles titles, Date conversionDate) {
		Logger.Info("Removing employer id from landed characters...");
		var landedCharacterIds = titles.GetHolderIds(conversionDate);
		foreach (var character in this.Where(character => landedCharacterIds.Contains(character.Id))) {
			character.History.Fields["employer"].RemoveAllEntries();
		}

		Logger.IncrementProgress();
	}

	/// <summary>
	/// Distributes Imperator countries' gold among rulers and governors
	/// </summary>
	/// <param name="titles">Landed titles collection</param>
	/// <param name="config">Current configuration</param>
	public void DistributeCountriesGold(Title.LandedTitles titles, Configuration config) {
		static void AddGoldToCharacter(Character character, double gold) {
			if (character.Gold is null) {
				character.Gold = gold;
			} else {
				character.Gold += gold;
			}
		}

		Logger.Info("Distributing countries' gold...");

		var bookmarkDate = config.CK3BookmarkDate;
		var ck3CountriesFromImperator = titles.GetCountriesImportedFromImperator();
		foreach (var ck3Country in ck3CountriesFromImperator) {
			var rulerId = ck3Country.GetHolderId(bookmarkDate);
			if (rulerId == "0") {
				Logger.Debug($"Can't distribute gold in {ck3Country} because it has no holder.");
				continue;
			}

			var imperatorGold = ck3Country.ImperatorCountry!.Currencies.Gold * config.ImperatorCurrencyRate;

			var vassalCharacterIds = ck3Country.GetDeFactoVassals(bookmarkDate).Values
				.Where(vassalTitle => !vassalTitle.Landless)
				.Select(vassalTitle => vassalTitle.GetHolderId(bookmarkDate))
				.ToHashSet();

			var vassalCharacters = new HashSet<Character>();
			foreach (var vassalCharacterId in vassalCharacterIds) {
				if (TryGetValue(vassalCharacterId, out var vassalCharacter)) {
					vassalCharacters.Add(vassalCharacter);
				} else {
					Logger.Warn($"Character {vassalCharacterId} not found!");
				}
			}

			// Ruler should also get a share, he has double weight, so we add 2 to the count.
			var mouthsToFeedCount = vassalCharacters.Count + 2;

			var goldPerVassal = imperatorGold / mouthsToFeedCount;
			foreach (var vassalCharacter in vassalCharacters) {
				AddGoldToCharacter(vassalCharacter, goldPerVassal);
				imperatorGold -= goldPerVassal;
			}

			var ruler = this[rulerId];
			AddGoldToCharacter(ruler, imperatorGold);
		}

		Logger.IncrementProgress();
	}

	public void ImportLegions(
		Title.LandedTitles titles,
		UnitCollection imperatorUnits,
		Imperator.Characters.CharacterCollection imperatorCharacters,
		Date date,
		UnitTypeMapper unitTypeMapper,
		IdObjectCollection<string, MenAtArmsType> menAtArmsTypes,
		ProvinceMapper provinceMapper,
		Configuration config
	) {
		Logger.Info("Importing Imperator armies...");

		var ck3CountriesFromImperator = titles.GetCountriesImportedFromImperator();
		foreach (var ck3Country in ck3CountriesFromImperator) {
			var rulerId = ck3Country.GetHolderId(date);
			if (rulerId == "0") {
				Logger.Debug($"Can't add armies to {ck3Country} because it has no holder.");
				continue;
			}

			var imperatorCountry = ck3Country.ImperatorCountry!;
			var countryLegions = imperatorUnits.Where(u => u.CountryId == imperatorCountry.Id)
				.Where(unit => unit.IsArmy && unit.IsLegion) // drop navies and levies
				.ToList();
			if (!countryLegions.Any()) {
				continue;
			}

			var ruler = this[rulerId];

			if (config.LegionConversion == LegionConversion.MenAtArms) {
				ruler.ImportUnitsAsMenAtArms(countryLegions, date, unitTypeMapper, menAtArmsTypes);
			} else if (config.LegionConversion == LegionConversion.SpecialTroops) {
				ruler.ImportUnitsAsSpecialTroops(countryLegions, imperatorCharacters, date, unitTypeMapper, provinceMapper);
			}
		}

		Logger.IncrementProgress();
	}
}