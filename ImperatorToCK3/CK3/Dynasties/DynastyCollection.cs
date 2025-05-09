﻿using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Culture;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImperatorToCK3.CK3.Dynasties;

internal sealed class DynastyCollection : ConcurrentIdObjectCollection<string, Dynasty> {
	public void ImportImperatorFamilies(Imperator.World irWorld, CultureMapper cultureMapper, LocDB irLocDB, CK3LocDB ck3LocDB, Date date) {
		Logger.Info("Importing Imperator families...");

		var imperatorCharacters = irWorld.Characters;
		// The collection only holds dynasties converted from Imperator families, as vanilla ones aren't modified.
		int importedCount = 0;
		var majorFamilies = irWorld.Families.Where(f => !f.Minor).ToArray();
		Parallel.ForEach(majorFamilies, family => {
			var newDynasty = new Dynasty(family, imperatorCharacters, irWorld.CulturesDB, cultureMapper, irLocDB, ck3LocDB, date);
			AddOrReplace(newDynasty);
			Interlocked.Increment(ref importedCount);
		});
		Logger.Info($"{importedCount} total families imported.");

		CreateDynastiesForCharactersFromMinorFamilies(irWorld, irLocDB, ck3LocDB, date);

		Logger.IncrementProgress();
	}

	public void LoadCK3Dynasties(ModFilesystem ck3ModFS) {
		Logger.Info("Loading dynasties from CK3...");

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, dynastyId) => {
			var dynasty = new Dynasty(dynastyId, reader);
			AddOrReplace(dynasty);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseGameFolder("common/dynasties", ck3ModFS, "txt", recursive: true);
	}

	private void CreateDynastiesForCharactersFromMinorFamilies(Imperator.World irWorld, LocDB irLocDB, CK3LocDB ck3LocDB, Date date) {
		Logger.Info("Creating dynasties for characters from minor families...");

		var relevantImperatorCharacters = irWorld.Characters
			.Where(c => c.CK3Character is not null && (c.Family is null || c.Family.Minor))
			.OrderBy(c => c.Id)
			.ToArray();

		int createdDynastiesCount = 0;
		foreach (var irCharacter in relevantImperatorCharacters) {
			var irFamilyName = irCharacter.FamilyName;
			if (string.IsNullOrEmpty(irFamilyName)) {
				continue;
			}

			var ck3Character = irCharacter.CK3Character!;
			if (ck3Character.GetDynastyId(date) is not null) {
				continue;
			}

			var ck3Father = ck3Character.Father;
			if (ck3Father is not null) {
				var fatherDynastyId = ck3Father.GetDynastyId(date);
				if (fatherDynastyId is not null) {
					ck3Character.SetDynastyId(fatherDynastyId, null);
					continue;
				}
			}

			// Neither character nor their father have a dynasty, so we need to create a new one.
			Imperator.Characters.Character[] irFamilyMembers = [irCharacter];
			var newDynasty = new Dynasty(ck3Character, irFamilyName, irFamilyMembers, irWorld.CulturesDB, irLocDB, ck3LocDB, date);
			AddOrReplace(newDynasty);
			++createdDynastiesCount;
		}

		Logger.Info($"Created {createdDynastiesCount} dynasties for characters from minor families.");
	}

	public void SetCoasForRulingDynasties(Title.LandedTitles titles, Date date) {
		Logger.Info("Setting dynasty CoAs from titles...");
		foreach (var title in titles.Where(t => t.CoA is not null && t.ImperatorCountry is not null)) {
			var dynastyId = title.ImperatorCountry!.Monarch?.CK3Character?.GetDynastyId(date);

			// Try to use title CoA for dynasty CoA.
			if (dynastyId is not null && TryGetValue(dynastyId, out var dynasty) && dynasty.CoA is null) {
				dynasty.CoA = new StringOfItem(title.Id);
			}
		}
		Logger.IncrementProgress();
	}

	public void PurgeUnneededDynasties(CharacterCollection characters, HouseCollection houses, Date date) {
		Logger.Info("Purging unneeded dynasties...");

		HashSet<string> dynastiesToKeep = [];
		
		// Load from configurable first.
		var nonRemovableIdsParser = new Parser();
		nonRemovableIdsParser.RegisterRegex(CommonRegexes.String, (_, id) => {
			dynastiesToKeep.Add(id);
		});
		nonRemovableIdsParser.IgnoreAndLogUnregisteredItems();
		nonRemovableIdsParser.ParseFile("configurables/dynasties_to_preserve.txt");
		
		foreach (var character in characters) {
			var dynastyIdAtBirth = character.GetDynastyId(character.BirthDate);
			if (dynastyIdAtBirth is not null) {
				dynastiesToKeep.Add(dynastyIdAtBirth);
			}
			
			var dynastyId = character.GetDynastyId(date);
			if (dynastyId is not null) {
				dynastiesToKeep.Add(dynastyId);
			}
		}

		foreach (var house in houses) {
			var dynastyId = house.DynastyId;
			if (dynastyId is not null) {
				dynastiesToKeep.Add(dynastyId);
			}
		}

		int removedCount = 0;
		foreach (var dynasty in this.ToArray()) {
			if (!dynastiesToKeep.Contains(dynasty.Id)) {
				Remove(dynasty.Id);
				++removedCount;
			}
		}

		Logger.Info($"Purged {removedCount} unneeded dynasties.");
	}

	/// <summary>
	/// In CK3, a dynasty without a pre-defined CoA should have a character in the main branch, acting as the founder.
	/// If there are dynasties that are missing a founder, this method will move all the characters from the cadet houses to the main branch.
	/// </summary>
	public void FlattenDynastiesWithNoFounders(CharacterCollection characters, HouseCollection houses, Date date) {
		Logger.Info("Flattening dynasties with no founders...");
		int count = 0;
		
		var charactersWithDynastyIds = characters
			.Select(c => (c, c.GetDynastyId(date)))
			.Where(c => c.Item2 is not null)
			.Select(c => (c.c, c.Item2!))
			.ToArray();
		var charactersWithHouseIds = characters
			.Select(c => (c, c.GetDynastyHouseId(date)))
			.Where(c => c.Item2 is not null)
			.Select(c => (c.c, c.Item2!))
			.ToArray();
		
		foreach (var dynasty in this) {
			var mainBranchMembers = charactersWithDynastyIds
				.Where(c => c.Item2 == dynasty.Id)
				.ToArray();
			if (mainBranchMembers.Length > 0) {
				continue;
			}
			
			var dynastyHouseIds = houses
				.Where(h => h.DynastyId == dynasty.Id)
				.Select(h => h.Id)
				.ToArray();
			var cadetHouseMembers = charactersWithHouseIds
				.Where(c => dynastyHouseIds.Contains(c.Item2))
				.Select(c => c.c)
				.ToArray();
			
			if (cadetHouseMembers.Length == 0) {
				continue;
			}
			
			foreach (var character in cadetHouseMembers) {
				character.ClearDynastyHouse();
				character.SetDynastyId(dynasty.Id, null);
			}
			
			// Remove all the cadet houses.
			foreach (var houseId in dynastyHouseIds) {
				houses.Remove(houseId);
			}
			
			++count;
		}
		
		Logger.Info($"Flattened {count} dynasties with no founders.");
	}
}