using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using ImperatorToCK3.CK3.Armies;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CK3.Dynasties;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Armies;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.Trait;
using ImperatorToCK3.Mappers.UnitType;
using Open.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImperatorToCK3.CK3.Characters;

public partial class CharacterCollection : ConcurrentIdObjectCollection<string, Character> {
	public void ImportImperatorCharacters(
		Imperator.World impWorld,
		ReligionMapper religionMapper,
		CultureMapper cultureMapper,
		CultureCollection ck3Cultures,
		TraitMapper traitMapper,
		NicknameMapper nicknameMapper,
		ProvinceMapper provinceMapper,
		DeathReasonMapper deathReasonMapper,
		DNAFactory dnaFactory,
		Date conversionDate,
		Configuration config
	) {
		Logger.Info("Importing Imperator Characters...");

		var unlocalizedImperatorNames = new ConcurrentHashSet<string>();

		Parallel.ForEach(impWorld.Characters, irCharacter => {
			ImportImperatorCharacter(
				irCharacter,
				religionMapper,
				cultureMapper,
				traitMapper,
				nicknameMapper,
				impWorld.LocDB,
				impWorld.MapData,
				provinceMapper,
				deathReasonMapper,
				dnaFactory,
				conversionDate,
				config,
				unlocalizedImperatorNames
			);
		});
		if (unlocalizedImperatorNames.Any()) {
			Logger.Warn("Found unlocalized Imperator names: " + string.Join(", ", unlocalizedImperatorNames));
		}
		Logger.Info($"Imported {impWorld.Characters.Count} characters.");

		LinkMothersAndFathers();
		LinkSpouses(conversionDate);
		LinkPrisoners(conversionDate);

		ImportFriendships(impWorld.Characters, conversionDate);
		ImportRivalries(impWorld.Characters, conversionDate);
		Logger.IncrementProgress();
		
		ImportPregnancies(impWorld.Characters, conversionDate);

		if (config.FallenEagleEnabled) {
			SetCharacterCastes(ck3Cultures, config.CK3BookmarkDate);
		}
	}

	private void ImportImperatorCharacter(
		Imperator.Characters.Character irCharacter,
		ReligionMapper religionMapper,
		CultureMapper cultureMapper,
		TraitMapper traitMapper,
		NicknameMapper nicknameMapper,
		LocDB locDB,
		MapData irMapData,
		ProvinceMapper provinceMapper,
		DeathReasonMapper deathReasonMapper,
		DNAFactory dnaFactory,
		Date endDate,
		Configuration config,
		ISet<string> unlocalizedImperatorNames) {
		// Create a new CK3 character.
		var newCharacter = new Character(
			irCharacter,
			this,
			religionMapper,
			cultureMapper,
			traitMapper,
			nicknameMapper,
			locDB,
			irMapData,
			provinceMapper,
			deathReasonMapper,
			dnaFactory,
			endDate,
			config,
			unlocalizedImperatorNames
		);
		irCharacter.CK3Character = newCharacter;
		AddOrReplace(newCharacter);
	}

	public override void Remove(string key) {
		BulkRemove([key]);
	}
	
	private void BulkRemove(ICollection<string> keys) {
		foreach (var key in keys) {
			var characterToRemove = this[key];

			characterToRemove.RemoveAllSpouses();
			characterToRemove.RemoveAllChildren();

			var irCharacter = characterToRemove.ImperatorCharacter;
			if (irCharacter is not null) {
				irCharacter.CK3Character = null;
			}
		
			base.Remove(key);
		}
		
		RemoveCharacterReferencesFromHistory(keys);
	}

	private void RemoveCharacterReferencesFromHistory(ICollection<string> idsToRemove) {
		var idsCapturingGroup = "(" + string.Join('|', idsToRemove) + ")";

		const string commandsCapturingGroup = "(" +
			"set_relation_rival|set_relation_potential_rival|set_relation_nemesis|" +
			"set_relation_lover|set_relation_soulmate|" +
			"set_relation_friend|set_relation_potential_friend|set_relation_best_friend|" +
			"set_relation_ward|set_relation_mentor)";

		var regex = new Regex(commandsCapturingGroup + @"\s*=\s*\{[^\}]*character:" + idsCapturingGroup + @"\s[^\}]*\}(?:\s*#.*)?");

		foreach (var character in this) {
			var effectsHistoryField = character.History.Fields["effects"];
			if (effectsHistoryField is not LiteralHistoryField effectsLiteralField) {
				Logger.Warn($"Effects history field for character {character.Id} is not a literal field!");
				continue;
			}
			if (effectsLiteralField.EntriesCount == 0) {
				continue;
			}

			effectsLiteralField.RegexReplaceAllEntries(regex, string.Empty);

			// Remove all empty effect blocks (effect = { }).
			effectsHistoryField.RemoveAllEntries(entryValue => {
				if (entryValue is not string valueString) {
					return false;
				}

				string trimmedBlock = valueString.Trim();
				if (!trimmedBlock.StartsWith('{') || !trimmedBlock.EndsWith('}')) {
					return false;
				}

				return trimmedBlock[1..^1].Trim().Length == 0;
			});
		}
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
				// Make sure to only add this relation once.
				if (irCharacter.Id.CompareTo(irFriendId) > 0) {
					continue;
				}

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
				// Make sure to only add this relation once.
				if (irCharacter.Id.CompareTo(irRivalId) > 0) {
					continue;
				}

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

	private void SetCharacterCastes(CultureCollection cultures, Date ck3BookmarkDate) {
		var casteSystemCultureIds = cultures
			.Where(c => c.TraditionIds.Contains("tradition_caste_system"))
			.Select(c => c.Id)
			.ToHashSet();
		var learningEducationTraits = new[]{"education_learning_1", "education_learning_2", "education_learning_3", "education_learning_4"};
		
		foreach (var character in this.OrderBy(c => c.BirthDate)) {
			if (character.ImperatorCharacter is null) {
				continue;
			}
			
			var cultureId = character.GetCultureId(ck3BookmarkDate);
			if (cultureId is null || !casteSystemCultureIds.Contains(cultureId)) {
				continue;
			}
			
			// The caste is hereditary.
			var father = character.Father;
			if (father is not null) {
				var foundTrait = GetCasteTraitFromParent(father);
				if (foundTrait is not null) {
					character.AddBaseTrait(foundTrait);
					continue;
				}
			}
			var mother = character.Mother;
			if (mother is not null) {
				var foundTrait = GetCasteTraitFromParent(mother);
				if (foundTrait is not null) {
					character.AddBaseTrait(foundTrait);
					continue;
				}
			}
			
			// Try to set caste based on character's traits.
			var traitIds = character.BaseTraits.ToHashSet();
			character.AddBaseTrait(traitIds.Intersect(learningEducationTraits).Any() ? "brahmin" : "kshatriya");
		}
		return;

		static string? GetCasteTraitFromParent(Character parentCharacter) {
			var casteTraits = new[]{"brahmin", "kshatriya", "vaishya", "shudra"};
			var parentTraitIds = parentCharacter.BaseTraits.ToHashSet();
			return casteTraits.Intersect(parentTraitIds).FirstOrDefault();
		}
	}

	private static IEnumerable<string> LoadCharacterIDsToPreserve() {
		Logger.Debug("Loading IDs of CK3 characters to preserve...");
		HashSet<string> characterIDsToPreserve = [];

		string configurablePath = "configurables/ck3_characters_to_preserve.txt";
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (_, id) => {
			characterIDsToPreserve.Add(id);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(configurablePath);

		return characterIDsToPreserve;
	}

	public void PurgeUnneededCharacters(Title.LandedTitles titles, DynastyCollection dynasties, Date ck3BookmarkDate) {
		Logger.Info("Purging unneeded characters...");
		
		// Characters that hold or held titles should always be kept.
		var landedCharacterIds = titles.GetAllHolderIds();
		var landedCharacters = this
			.Where(character => landedCharacterIds.Contains(character.Id))
			.ToList();
		var charactersToCheck = this.Except(landedCharacters);
		
		// Don't purge animation_test or easter egg characters.
		charactersToCheck = charactersToCheck
			.Where(c => !c.Id.StartsWith("animation_test_") && !c.Id.StartsWith("easteregg_"));
		
		// Keep alive Imperator characters.
		charactersToCheck = charactersToCheck
			.Where(c => c is not {FromImperator: true, Dead: false});
				
		// Make some exceptions for characters referenced in game's script files.
		var characterIdsToKeep = LoadCharacterIDsToPreserve();

		charactersToCheck = charactersToCheck
			.Where(character => !characterIdsToKeep.Contains(character.Id))
			.ToList();

		var dynastyIdsOfLandedCharacters = landedCharacters
			.Select(character => character.GetDynastyId(ck3BookmarkDate))
			.Distinct()
			.ToHashSet();
		
		var i = 0;
		var charactersToRemove = new List<Character>();
		var parentIdsCache = new HashSet<string>();
		do {
			Logger.Debug($"Beginning iteration {i} of characters purge...");
			charactersToRemove.Clear();
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
				// Does the character belong to a dynasty that holds or held titles?
				if (dynastyIdsOfLandedCharacters.Contains(character.GetDynastyId(ck3BookmarkDate))) {
					// Is the character dead and childless? Purge.
					if (!parentIdsCache.Contains(character.Id)) {
						charactersToRemove.Add(character);
					}

					continue;
				}

				charactersToRemove.Add(character);
			}
			
			BulkRemove(charactersToRemove.Select(c => c.Id).ToList());

			Logger.Debug($"\tPurged {charactersToRemove.Count} unneeded characters in iteration {i}.");
			charactersToCheck = charactersToCheck.Except(charactersToRemove).ToList();
		} while(charactersToRemove.Count > 0);
		
		// At this point we probably have many imported dynasties with no characters left.
		// Let's purge them.
		dynasties.PurgeUnneededDynasties(this, ck3BookmarkDate);
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