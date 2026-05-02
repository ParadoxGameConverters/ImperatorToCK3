using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Characters;
using System;
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

	public void MergeDividedFamilies(CharacterCollection characters) {
		Logger.Info("Merging divided families...");

		var keyCounts = new Dictionary<string, int>();
		foreach (var family in this) {
			if (string.IsNullOrEmpty(family.Key)) {
				continue;
			}
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
		if (familiesPerKey.Count == 0) {
			Logger.IncrementProgress();
			return;
		}

		var familyIdsEligibleForMerging = new HashSet<ulong>();
		var memberIdToFamily = new Dictionary<ulong, Family>();
		foreach (var groupedFamilies in familiesPerKey.Values) {
			foreach (var family in groupedFamilies) {
				familyIdsEligibleForMerging.Add(family.Id);
				foreach (var memberId in family.MemberIds) {
					memberIdToFamily[memberId] = family;
				}
			}
		}

		var disjointSet = new Dictionary<ulong, ulong>();
		foreach (var familyId in familyIdsEligibleForMerging) {
			disjointSet[familyId] = familyId;
		}

		foreach (var character in characters) {
			if (!memberIdToFamily.TryGetValue(character.Id, out var characterFamily) || !familyIdsEligibleForMerging.Contains(characterFamily.Id)) {
				continue;
			}

			TryUnionFamilies(characterFamily, character.Father, memberIdToFamily, disjointSet);
			TryUnionFamilies(characterFamily, character.Mother, memberIdToFamily, disjointSet);
		}

		foreach (var (groupingKey, groupingFamilies) in familiesPerKey) {
			var survivingFamiliesByRootId = new Dictionary<ulong, Family>();
			var mergedFamiliesCount = 0;
			foreach (var family in groupingFamilies) {
				var rootId = FindRootFamilyId(family.Id, disjointSet);
				if (!survivingFamiliesByRootId.TryGetValue(rootId, out var survivingFamily)) {
					survivingFamiliesByRootId[rootId] = family;
					continue;
				}
				if (ReferenceEquals(survivingFamily, family)) {
					continue;
				}

				survivingFamily.MemberIds.UnionWith(family.MemberIds);
				foreach (var memberId in family.MemberIds) {
					if (characters.TryGetValue(memberId, out var familyMember)) {
						familyMember.Family = survivingFamily;
					}
				}
				Remove(family.Id);
				++mergedFamiliesCount;
			}

			if (mergedFamiliesCount > 0) {
				Logger.Debug($"Reunited {mergedFamiliesCount} divided families for key {groupingKey}.");
			}
		}

		Logger.IncrementProgress();
	}

	private static void TryUnionFamilies(Family family, Character? parentCharacter, Dictionary<ulong, Family> memberIdToFamily, Dictionary<ulong, ulong> disjointSet) {
		if (parentCharacter is null || !memberIdToFamily.TryGetValue(parentCharacter.Id, out var parentFamily) || family.Id == parentFamily.Id || family.Key != parentFamily.Key) {
			return;
		}
		if (!disjointSet.ContainsKey(parentFamily.Id)) {
			return;
		}

		var familyRootId = FindRootFamilyId(family.Id, disjointSet);
		var parentRootId = FindRootFamilyId(parentFamily.Id, disjointSet);
		if (familyRootId == parentRootId) {
			return;
		}

		disjointSet[parentRootId] = familyRootId;
	}

	private static ulong FindRootFamilyId(ulong familyId, Dictionary<ulong, ulong> disjointSet) {
		var rootId = familyId;
		while (disjointSet[rootId] != rootId) {
			rootId = disjointSet[rootId];
		}

		while (disjointSet[familyId] != familyId) {
			var parentId = disjointSet[familyId];
			disjointSet[familyId] = rootId;
			familyId = parentId;
		}

		return rootId;
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