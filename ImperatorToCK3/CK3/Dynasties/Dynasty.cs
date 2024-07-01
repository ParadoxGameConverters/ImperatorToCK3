using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using commonItems.Serialization;
using commonItems.SourceGenerators;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Cultures;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.Mappers.Culture;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Dynasties;

[SerializationByProperties]
public sealed partial class Dynasty : IPDXSerializable, IIdentifiable<string> {
	public Dynasty(Family irFamily, CharacterCollection irCharacters, CulturesDB irCulturesDB, CultureMapper cultureMapper, LocDB irLocDB, Date date) {
		FromImperator = true;
		Id = $"dynn_irtock3_{irFamily.Id}";
		Name = Id;

		var imperatorMemberIds = irFamily.MemberIds;
		var imperatorMembers = irCharacters
			.Where(c => imperatorMemberIds.Contains(c.Id))
			.ToArray();

		SetCultureFromImperator(irFamily, imperatorMembers, cultureMapper, date);

		foreach (var member in imperatorMembers) {
			var ck3Member = member.CK3Character;
			ck3Member?.SetDynastyId(Id, date: null);
		}
		
		SetLocFromImperatorFamilyName(irFamily.GetMaleForm(irCulturesDB), irLocDB);
	}

	public Dynasty(CK3.Characters.Character character, string irFamilyName, CulturesDB irCulturesDB, LocDB irLocDB, Date date) {
		FromImperator = true;
		Id = $"dynn_irtock3_from_{character.Id}";
		Name = Id;

		CultureId = character.GetCultureId(date) ?? character.Father?.GetCultureId(date);
		if (CultureId is null) {
			Logger.Warn($"Couldn't determine culture for dynasty {Id}, needs manual setting!");
		}
		
		character.SetDynastyId(Id, null);
		
		SetLocFromImperatorFamilyName(Family.GetMaleForm(irFamilyName, irCulturesDB), irLocDB);
	}
	
	public Dynasty(string dynastyId, BufferedReader dynastyReader) {
		Id = dynastyId;
		FromImperator = false;

		var parser = new Parser();
		parser.RegisterKeyword("prefix", reader => Prefix = reader.GetString());
		parser.RegisterKeyword("name", reader => Name = reader.GetString());
		parser.RegisterKeyword("culture", reader => CultureId = reader.GetString());
		parser.RegisterKeyword("motto", reader => Motto = reader.GetString());
		parser.RegisterKeyword("forced_coa_religiongroup", reader => ForcedCoaReligionGroup = reader.GetString());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(dynastyReader);
		
		if (string.IsNullOrEmpty(Name)) {
			Logger.Warn($"Dynasty {Id} has no name! Setting fallback unlocalized name.");
			Name = Id;
		}
	}
	
	[NonSerialized] public string Id { get; }
	[SerializedName("prefix")] public string? Prefix { get; private set; }
	[SerializedName("name")] public string Name { get; private set; }
	[SerializedName("culture")] public string? CultureId { get; set; }
	[SerializedName("motto")] public string? Motto { get; set; }
	[SerializedName("forced_coa_religiongroup")] public string? ForcedCoaReligionGroup { get; set; }

	[NonSerialized] public LocBlock? LocalizedName { get; private set; }
	[NonSerialized] public StringOfItem? CoA { get; set; }
	[NonSerialized] public bool FromImperator { get; private set; } = false;

	private void SetCultureFromImperator(Family irFamily, IReadOnlyList<Character> irMembers, CultureMapper cultureMapper, Date date) {
		if (irMembers.Count > 0) {
			var firstImperatorMember = irMembers[0];
			// Try to make head's culture the dynasty culture.
			if (firstImperatorMember.CK3Character is not null) {
				CultureId = firstImperatorMember.CK3Character.GetCultureId(date);
				return;
			}

			// Try to set culture from other members.
			var otherImperatorMembers = irMembers.Skip(1).ToArray();
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
			.FirstOrDefault(id => id.HasValue);
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

	private void SetLocFromImperatorFamilyName(string irFamilyLocKey, LocDB irLocDB) {
		var irFamilyLoc = irLocDB.GetLocBlockForKey(irFamilyLocKey);
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