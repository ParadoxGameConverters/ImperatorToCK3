using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Characters;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Families; 

public class FamilyCollection : IdObjectCollection<ulong, Family> {
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
		var familiesPerKey = this.GroupBy(f => f.Key);
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
				var familyMembers = characters
					.Where(c => familyMemberIds.Contains(c.Id))
					.ToList();
				foreach (var anotherFamily in grouping) {
					if (family.Equals(anotherFamily)) {
						continue;
					}

					var anotherFamilyMemberIds = anotherFamily.MemberIds;
					var anotherFamilyMembers = characters
						.Where(c => anotherFamilyMemberIds.Contains(c.Id))
						.ToList();
					if (familyMembers.Any(c =>
						    (c.Father is Character father && anotherFamilyMemberIds.Contains(father.Id)) ||
						    (c.Mother is Character mother && anotherFamilyMemberIds.Contains(mother.Id))
					    )
					) {
						Logger.Debug($"Reuniting family {grouping.Key}: {anotherFamily.Id} into {family.Id}");
						ReuniteFamily(family, anotherFamily, anotherFamilyMembers);
						removedFamilies.Add(anotherFamily);
					}
				}
			}
		}

		Logger.IncrementProgress();
	}

	public static FamilyCollection ParseBloc(BufferedReader reader) {
		var blocParser = new Parser();
		var families = new FamilyCollection();
		blocParser.RegisterKeyword("families", reader =>
			families.LoadFamilies(reader)
		);
		blocParser.IgnoreAndLogUnregisteredItems();
		blocParser.ParseStream(reader);

		Logger.Debug($"Ignored family tokens: {string.Join(", ", Family.IgnoredTokens)}");
		return families;
	}
}