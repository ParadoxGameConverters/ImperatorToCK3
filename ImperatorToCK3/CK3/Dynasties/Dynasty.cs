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
		Id = $"dynn_IMPTOCK3_{imperatorFamily.Id}";
		Name = Id;

		var imperatorMemberIds = imperatorFamily.MemberIds;
		var imperatorMembers = imperatorCharacters.Where(c => imperatorMemberIds.Contains(c.Id)).ToList();
		if (imperatorMembers.Count > 0) {
			var firstImperatorMember = imperatorMembers[0];
			if (firstImperatorMember.CK3Character is not null) {
				// Make head's culture the dynasty culture.
				Culture = firstImperatorMember.CK3Character.CultureId;
			}
		} else {
			Logger.Warn($"Couldn't determine culture for dynasty {Id}, needs manual setting!");
		}

		foreach (var member in imperatorMembers) {
			var ck3Member = member.CK3Character;
			if (ck3Member is not null) {
				ck3Member.DynastyId = Id;
			}
		}

		var impFamilyLocKey = imperatorFamily.GetMaleForm(irCulturesDB);
		var impFamilyLoc = locDB.GetLocBlockForKey(impFamilyLocKey);
		if (impFamilyLoc is not null) {
			Localization = new(Name, impFamilyLoc);
		} else { // fallback: use unlocalized Imperator family key
			var locBlock = new LocBlock(Name, "english") {
				["english"] = impFamilyLocKey
			};
			Localization = new(Name, locBlock);
		}
	}
	[NonSerialized] public string Id { get; }
	[SerializedName("name")] public string Name { get; }
	[SerializedName("culture")] public string? Culture { get; set; }

	[NonSerialized] public KeyValuePair<string, LocBlock> Localization { get; } = new();
	[NonSerialized] public StringOfItem? CoA { get; set; }
}