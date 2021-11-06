using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.Mappers.Localization;
using System.Collections.Generic;
// ReSharper disable InconsistentNaming

namespace ImperatorToCK3.CK3.Dynasties {
	public class Dynasty : IPDXSerializable {
		public Dynasty(Family imperatorFamily, LocalizationMapper localizationMapper) {
			Id = "dynn_IMPTOCK3_" + imperatorFamily.Id;
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
			var impFamilyLoc = localizationMapper.GetLocBlockForKey(impFamilyLocKey);
			if (impFamilyLoc is not null) {
				Localization = new(Name, impFamilyLoc);
			} else { // fallback: use unlocalized Imperator family key
				Localization = new(Name, new LocBlock {
					english = impFamilyLocKey,
					french = impFamilyLocKey,
					german = impFamilyLocKey,
					russian = impFamilyLocKey,
					simp_chinese = impFamilyLocKey,
					spanish = impFamilyLocKey
				});
			}
		}
		[NonSerialized] public string Id { get; } = string.Empty;
		[SerializedName("name")] public string Name { get; } = string.Empty;
		[SerializedName("culture")] public string Culture { get; private set; } = string.Empty;

		[NonSerialized] public KeyValuePair<string, LocBlock> Localization { get; } = new();
	}
}
