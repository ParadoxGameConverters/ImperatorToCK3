using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using commonItems.Serialization;
using ImperatorToCK3.Imperator.Families;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Dynasties;

public class Dynasty : IPDXSerializable, IIdentifiable<string> {
	public Dynasty(Family imperatorFamily, LocDB locDB) {
		Id = $"dynn_IMPTOCK3_{imperatorFamily.Id}";
		Name = Id;

		var imperatorMembers = imperatorFamily.Members;
		if (imperatorMembers.Count > 0) {
			Imperator.Characters.Character? firstMember = imperatorMembers[0] as Imperator.Characters.Character;
			if (firstMember?.CK3Character is not null) {
				Culture = firstMember.CK3Character.Culture; // make head's culture the dynasty culture
			}
		} else {
			Logger.Warn($"Couldn't determine culture for dynasty {Id}, needs manual setting!");
		}

		foreach (var member in imperatorMembers.Values) {
			var ck3Member = (member as Imperator.Characters.Character)?.CK3Character;
			if (ck3Member is not null) {
				ck3Member.DynastyId = Id;
			}
		}

		var impFamilyLocKey = imperatorFamily.Key;
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
}