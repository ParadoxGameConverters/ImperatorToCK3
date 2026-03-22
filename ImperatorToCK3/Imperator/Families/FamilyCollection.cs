using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Characters;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Families;

internal sealed class FamilyCollection : IdObjectCollection<ulong, Family> {
	public void LoadFamiliesFromBloc(BufferedReader reader) {
		var blocParser = new Parser(implicitVariableHandling: false);
		blocParser.RegisterKeyword("families", LoadFamilies);
		blocParser.IgnoreAndLogUnregisteredItems();
		blocParser.ParseStream(reader);

		Logger.Debug($"Ignored family tokens: {Family.IgnoredTokens}");
	}
	public void LoadFamilies(BufferedReader reader) {
		var parser = new Parser(implicitVariableHandling: false);
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterRegex(CommonRegexes.Integer, (reader, familyIdStr) => {
			var familyStr = new StringOfItem(reader).ToString();
			if (!familyStr.Contains('{')) {
				return;
			}
			var tempReader = new BufferedReader(familyStr);
			var id = ulong.Parse(familyIdStr);
			var newFamily = Family.Parse(tempReader, id);
			var inserted = TryAdd(newFamily);
			if (!inserted) {
				Logger.Debug($"Redefinition of family {id}.");
				dict[newFamily.Id] = newFamily;
			}
		});
		parser.IgnoreAndLogUnregisteredItems();
	}
	public void RemoveUnlinkedMembers(CharacterCollection characters) {
		foreach (var family in this) {
			family.RemoveUnlinkedMembers(characters);
		}
	}

	private void ReuniteFamily(Family family, Family familyToBeMerged, Character[] charactersToReassign) {
		family.MemberIds.UnionWith(familyToBeMerged.MemberIds);
		foreach (var character in charactersToReassign) {
			character.Family = family;
		}

		Remove(familyToBeMerged.Id);
	}

	public void MergeDividedFamilies(CharacterCollection characters) {
		Logger.Info("Merging divided families...");
		
		Dictionary<ulong, Character[]> familyIdToCharactersCache = [];

		// Pre-compute the set of keys that have duplicate families.
		// Each iteration only re-groups families with those keys, skipping the rest.
		var keyCounts = new Dictionary<string, int>();
		foreach (var family in this) {
			if (keyCounts.TryGetValue(family.Key, out var count)) {
				keyCounts[family.Key] = count + 1;
			} else {
				keyCounts[family.Key] = 1;
			}
		}
		var duplicateKeys = new HashSet<string>();
		foreach (var (key, count) in keyCounts) {
			if (count > 1) {
				duplicateKeys.Add(key);
			}
		}

		var iteration = 0;
		bool anotherIterationNeeded = duplicateKeys.Count > 0;
		while (anotherIterationNeeded) {
			var familiesPerKey = new Dictionary<string, List<Family>>();
			foreach (var family in this) {
				if (!duplicateKeys.Contains(family.Key)) {
					continue;
				}

				if (!familiesPerKey.TryGetValue(family.Key, out var groupedFamilies)) {
					groupedFamilies = [];
					familiesPerKey[family.Key] = groupedFamilies;
				}
				groupedFamilies.Add(family);
			}
			anotherIterationNeeded = false;
			++iteration;
			Logger.Debug($"Family merging iteration {iteration}");

			foreach (var (groupingKey, groupingFamilies) in familiesPerKey) {
				if (groupingFamilies.Count <= 1) {
					continue;
				}

				var removedFamilies = new HashSet<Family>();
				foreach (var family in groupingFamilies) {
					if (removedFamilies.Contains(family)) {
						continue;
					}
					var familyMemberIds = family.MemberIds;
					foreach (var anotherFamily in groupingFamilies) {
						if (family.Equals(anotherFamily)) {
							continue;
						}

						var anotherFamilyMemberIds = anotherFamily.MemberIds;
						Character[] anotherFamilyMembers;
						if (familyIdToCharactersCache.TryGetValue(anotherFamily.Id, out var cachedMembers)) {
							anotherFamilyMembers = cachedMembers;
						}
						else {
							anotherFamilyMembers = [.. characters.Where(c => anotherFamilyMemberIds.Contains(c.Id))];
							familyIdToCharactersCache[anotherFamily.Id] = anotherFamilyMembers;
						}

						// Check if any parent of characters from "anotherFamily" belongs to "family".
						if (!anotherFamilyMembers.Any(c =>
							    (c.Father is Character father && familyMemberIds.Contains(father.Id)) ||
							    (c.Mother is Character mother && familyMemberIds.Contains(mother.Id))
						    )) {
							continue;
						}

						Logger.Debug($"Reuniting family {groupingKey}: {anotherFamily.Id} into {family.Id}");
						ReuniteFamily(family, anotherFamily, anotherFamilyMembers);
						removedFamilies.Add(anotherFamily);

						anotherIterationNeeded = true;
					}
				}
			}
		}

		Logger.IncrementProgress();
	}

	public void PurgeUnneededFamilies(CharacterCollection characters) {
		// Drop families with no members.
		var familiesIdToKeep = new HashSet<ulong>();
		foreach (var character in characters) {
			if (character.Family?.Id is ulong familyId) {
				familiesIdToKeep.Add(familyId);
			}
		}

		// Collect IDs to remove, then remove – avoids snapshotting the entire collection.
		var idsToRemove = this
			.Where(f => !familiesIdToKeep.Contains(f.Id))
			.Select(f => f.Id)
			.ToList();
		foreach (var id in idsToRemove) {
			Remove(id);
		}

		Logger.Info($"Purged {idsToRemove.Count} unneeded Imperator families.");
	}
}