﻿using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using commonItems.Serialization;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Cultures;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.Mappers.Culture;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Dynasties;

public class Dynasty : IPDXSerializable, IIdentifiable<string> {
	public Dynasty(Family irFamily, CharacterCollection irCharacters, CulturesDB irCulturesDB, CultureMapper cultureMapper, LocDB locDB) {
		Id = $"dynn_irtock3_{irFamily.Id}";
		Name = Id;

		var imperatorMemberIds = irFamily.MemberIds;
		var imperatorMembers = irCharacters
			.Where(c => imperatorMemberIds.Contains(c.Id))
			.ToList();

		SetCultureFromImperator(irFamily, imperatorMembers, cultureMapper);

		foreach (var member in imperatorMembers) {
			var ck3Member = member.CK3Character;
			if (ck3Member is not null) {
				ck3Member.DynastyId = Id;
			}
		}

		var irFamilyLocKey = irFamily.GetMaleForm(irCulturesDB);
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
	[SerializedName("culture")] public string? CultureId { get; set; }

	[NonSerialized] public LocBlock? LocalizedName { get; }
	[NonSerialized] public StringOfItem? CoA { get; set; }

	private void SetCultureFromImperator(Family irFamily, IReadOnlyList<Character> irMembers, CultureMapper cultureMapper) {
		if (irMembers.Count > 0) {
			var firstImperatorMember = irMembers[0];
			// Try to make head's culture the dynasty culture.
			if (firstImperatorMember.CK3Character is not null) {
				CultureId = firstImperatorMember.CK3Character.CultureId;
				return;
			}

			// Try to set culture from other members.
			var otherImperatorMembers = irMembers.Skip(1).ToList();
			foreach (var otherImperatorMember in otherImperatorMembers) {
				if (otherImperatorMember.CK3Character is null) {
					continue;
				}

				CultureId = otherImperatorMember.CK3Character.CultureId;
				return;
			}
		}

		// Try to set culture from family.
		var irCultureId = irFamily.Culture;
		var ck3CultureId = cultureMapper.NonReligiousMatch(irCultureId, string.Empty, 0, 0, string.Empty);
		if (ck3CultureId is not null) {
			CultureId = ck3CultureId;
			return;
		}

		Logger.Warn($"Couldn't determine culture for dynasty {Id}, needs manual setting!");
	}
}