using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using commonItems.Serialization;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Cultures;
using ImperatorToCK3.Imperator.Families;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Dynasties;

public class Dynasty : IPDXSerializable, IIdentifiable<string> {
	public Dynasty(Family imperatorFamily, CharacterCollection imperatorCharacters, CulturesDB irCulturesDB, LocDB locDB) {
		Id = $"dynn_irtock3_{imperatorFamily.Id}";
		Name = Id;
		
		var imperatorMemberIds = imperatorFamily.MemberIds;
		var imperatorMembers = imperatorCharacters
			.Where(c => imperatorMemberIds.Contains(c.Id))
			.ToList();

		SetCultureFromImperator(imperatorFamily, imperatorMembers);

		foreach (var member in imperatorMembers) {
			var ck3Member = member.CK3Character;
			if (ck3Member is not null) {
				ck3Member.DynastyId = Id;
			}
		}

		var irFamilyLocKey = imperatorFamily.GetMaleForm(irCulturesDB);
		var irFamilyLoc = locDB.GetLocBlockForKey(irFamilyLocKey);
		if (irFamilyLoc is not null) {
			LocalizedName = new LocBlock(Name, irFamilyLoc);
			LocalizedName.ModifyForEveryLanguage(irFamilyLoc, (orig, other, lang) => {
				if (!string.IsNullOrEmpty(orig)) {
					return orig;
				}
				return !string.IsNullOrEmpty(other) ? other : irFamilyLoc.Id;
			});
		} else { // fallback: use unlocalized Imperator family key
			LocalizedName = new LocBlock(Name, "english") {
				["english"] = irFamilyLocKey
			};
		}
	}
	[NonSerialized] public string Id { get; }
	[SerializedName("name")] public string Name { get; }
	[SerializedName("culture")] public string? Culture { get; set; }

	[NonSerialized] public LocBlock? LocalizedName { get; }
	[NonSerialized] public StringOfItem? CoA { get; set; }

	private void SetCultureFromImperator(Family irFamily, IReadOnlyList<Character> irMembers) {
		if (irMembers.Count > 0) {
			var firstImperatorMember = irMembers[0];
			if (firstImperatorMember.CK3Character is not null) {
				// Make head's culture the dynasty culture.
				Culture = firstImperatorMember.CK3Character.CultureId;
				return;
			}
			
			// Try to set culture from other members.
			var otherImperatorMembers = irMembers.Skip(1).ToList();
			foreach (var otherImperatorMember in otherImperatorMembers) {
				if (otherImperatorMember.CK3Character is null) {
					continue;
				}

				// Make member's culture the dynasty culture.
				Culture = otherImperatorMember.CK3Character.CultureId;
				return;
			}
		}
		
		// Try to set culture from family.
		if (irFamily.Culture is not null) {
			Culture = irFamily.Culture;
			return;
		}
		
		Logger.Warn($"Couldn't determine culture for dynasty {Id}, needs manual setting!");
	}
}