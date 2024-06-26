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

public sealed class DynastyCollection : ConcurrentIdObjectCollection<string, Dynasty> {
	public void ImportImperatorFamilies(Imperator.World irWorld, CultureMapper cultureMapper, LocDB irLocDB, Date date) {
		Logger.Info("Importing Imperator families...");

		var imperatorCharacters = irWorld.Characters;
		// The collection only holds dynasties converted from Imperator families, as vanilla ones aren't modified.
		int importedCount = 0;
		Parallel.ForEach(irWorld.Families, family => {
			if (family.Minor) {
				return;
			}

			var newDynasty = new Dynasty(family, imperatorCharacters, irWorld.CulturesDB, cultureMapper, irLocDB, date);
			AddOrReplace(newDynasty);
			Interlocked.Increment(ref importedCount);
		});
		Logger.Info($"{importedCount} total families imported.");

		CreateDynastiesForCharactersFromMinorFamilies(irWorld, irLocDB, date);

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
		parser.ParseGameFolder("common/dynasties", ck3ModFS, "txt", recursive: true, parallel: true);
	}

	private void CreateDynastiesForCharactersFromMinorFamilies(Imperator.World irWorld, LocDB irLocDB, Date date) {
		Logger.Info("Creating dynasties for characters from minor families...");

		var relevantImperatorCharacters = irWorld.Characters
			.Where(c => c.CK3Character is not null && c.Family?.Minor == true)
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
			var newDynasty = new Dynasty(ck3Character, irFamilyName, irWorld.CulturesDB, irLocDB, date);
			Add(newDynasty);
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

	public void PurgeUnneededDynasties(CharacterCollection characters, Date date) {
		Logger.Info("Purging unneeded dynasties...");

		HashSet<string> dynastiesToKeep = [];
		foreach (var character in characters) {
			var dynastyId = character.GetDynastyId(date);
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
}