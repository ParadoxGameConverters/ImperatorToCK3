using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Characters;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Families;

public sealed class FamilyCollection : IdObjectCollection<ulong, Family> {
	public void LoadFamiliesFromBloc(BufferedReader reader) {
		var blocParser = new Parser();
		blocParser.RegisterKeyword("families", LoadFamilies);
		blocParser.IgnoreAndLogUnregisteredItems();
		blocParser.ParseStream(reader);

		Logger.Debug($"Ignored family tokens: {Family.IgnoredTokens}");
	}
	public void LoadFamilies(BufferedReader reader) {
		var parser = new Parser();
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

	private void ReuniteFamily(Family family, Family familyToBeMerged, IEnumerable<Character> charactersToReassign) {
		family.MemberIds.UnionWith(familyToBeMerged.MemberIds);
		foreach (var character in charactersToReassign) {
			character.Family = family;
		}

		Remove(familyToBeMerged.Id);
	}

	public void MergeDividedFamilies(CharacterCollection characters) {
		Logger.Info("Merging divided families...");

		var iteration = 0;
		bool anotherIterationNeeded = true;
		while (anotherIterationNeeded) {
			var familiesPerKey = this.GroupBy(f => f.Key).ToArray();
			anotherIterationNeeded = false;
			++iteration;
			Logger.Debug($"Family merging iteration {iteration}");

			foreach (var grouping in familiesPerKey) {
				if (grouping.Count() < 2) {
					continue;
				}

				var removedFamilies = new HashSet<Family>();
				foreach (var family in grouping) {
					if (removedFamilies.Contains(family)) {
						continue;
					}
					var familyMemberIds = family.MemberIds;
					foreach (var anotherFamily in grouping) {
						if (family.Equals(anotherFamily)) {
							continue;
						}

						var anotherFamilyMemberIds = anotherFamily.MemberIds;
						var anotherFamilyMembers = characters
							.Where(c => anotherFamilyMemberIds.Contains(c.Id))
							.ToArray();

						// Check if any parent of characters from "anotherFamily" belongs to "family".
						if (!anotherFamilyMembers.Any(c =>
							    (c.Father is Character father && familyMemberIds.Contains(father.Id)) ||
							    (c.Mother is Character mother && familyMemberIds.Contains(mother.Id))
						    )) {
							continue;
						}

						Logger.Debug($"Reuniting family {grouping.Key}: {anotherFamily.Id} into {family.Id}");
						ReuniteFamily(family, anotherFamily, anotherFamilyMembers);
						removedFamilies.Add(anotherFamily);

						anotherIterationNeeded = true;
					}
				}
			}
		}

		Logger.IncrementProgress();
	}
}