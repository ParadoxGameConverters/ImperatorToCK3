using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using commonItems.Serialization;
using commonItems.SourceGenerators;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Cultures;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.Mappers.Culture;
using Open.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Dynasties;

[SerializationByProperties]
public partial class Dynasty : IPDXSerializable, IIdentifiable<string> {
	public Dynasty(Family irFamily, CharacterCollection irCharacters, CulturesDB irCulturesDB, CultureMapper cultureMapper, LocDB locDB, Date date) {
		Id = $"dynn_irtock3_{irFamily.Id}";
		Name = Id;

		var imperatorMemberIds = irFamily.MemberIds;
		var imperatorMembers = irCharacters
			.Where(c => imperatorMemberIds.Contains(c.Id))
			.ToList();

		SetCultureFromImperator(irFamily, imperatorMembers, cultureMapper, date);

		foreach (var member in imperatorMembers) {
			var ck3Member = member.CK3Character;
			ck3Member?.SetDynastyId(Id, null);
		}
		
		SetLocFromImperatorFamilyName(irFamily.GetMaleForm(irCulturesDB), locDB);
	}

	public Dynasty(CK3.Characters.Character character, string irFamilyName, CulturesDB irCulturesDB, LocDB locDB, Date date) {
		Id = $"dynn_irtock3_from_{character.Id}";
		Name = Id;

		CultureId = character.GetCultureId(date) ?? character.Father?.GetCultureId(date);
		if (CultureId is null) {
			Logger.Warn($"Couldn't determine culture for dynasty {Id}, needs manual setting!");
		}
		
		character.SetDynastyId(Id, null);
		
		SetLocFromImperatorFamilyName(Family.GetMaleForm(irFamilyName, irCulturesDB), locDB);
	}
	
	[NonSerialized] public string Id { get; }
	[SerializedName("name")] public string Name { get; }
	[SerializedName("culture")] public string? CultureId { get; set; }

	[NonSerialized] public LocBlock? LocalizedName { get; private set; }
	[NonSerialized] public StringOfItem? CoA { get; set; }

	private void SetCultureFromImperator(Family irFamily, IReadOnlyList<Character> irMembers, CultureMapper cultureMapper, Date date) {
		if (irMembers.Count > 0) {
			var firstImperatorMember = irMembers[0];
			// Try to make head's culture the dynasty culture.
			if (firstImperatorMember.CK3Character is not null) {
				CultureId = firstImperatorMember.CK3Character.GetCultureId(date);
				return;
			}

			// Try to set culture from other members.
			var otherImperatorMembers = irMembers.Skip(1).ToList();
			foreach (var otherImperatorMember in otherImperatorMembers) {
				if (otherImperatorMember.CK3Character is null) {
					continue;
				}

				CultureId = otherImperatorMember.CK3Character.GetCultureId(date);
				return;
			}
		}

		// Try to set culture from family.
		var irCultureId = irFamily.Culture;
		var irProvinceIdForMapping = irMembers
			.Select(m => m.ProvinceId)
			.Where(id => id != 0)
			.NullableFirstOrDefault();
		var countryTag = irMembers
			.Select(m => m.Country?.HistoricalTag)
			.FirstOrDefault(tag => tag is not null, defaultValue: null);
		var ck3CultureId = cultureMapper.Match(irCultureId, null, irProvinceIdForMapping, countryTag);
		if (ck3CultureId is not null) {
			CultureId = ck3CultureId;
			return;
		}

		Logger.Warn($"Couldn't determine culture for dynasty {Id}, needs manual setting!");
	}

	private void SetLocFromImperatorFamilyName(string irFamilyLocKey, LocDB locDB) {
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
			LocalizedName = new LocBlock(Name, ConverterGlobals.PrimaryLanguage) {
				[ConverterGlobals.PrimaryLanguage] = irFamilyLocKey
			};
		}
	}
}