using commonItems;
using commonItems.Localization;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Titles;

internal sealed class RulerTerm {
	public string? CharacterId { get; }
	public Date StartDate { get; }
	public string? Government { get; }

	public RulerTerm(
		Title ck3Title,
		Imperator.Countries.RulerTerm imperatorRulerTerm,
		Characters.CharacterCollection characters,
		GovernmentMapper governmentMapper,
		LocDB irLocDB,
		CK3LocDB ck3LocDB,
		ReligionMapper religionMapper,
		CultureMapper cultureMapper,
		NicknameMapper nicknameMapper,
		ProvinceMapper provinceMapper,
		Configuration config,
		IReadOnlyCollection<string> enabledCK3Dlcs) {
		if (imperatorRulerTerm.CharacterId is not null) {
			CharacterId = $"imperator{imperatorRulerTerm.CharacterId}";
		}
		StartDate = imperatorRulerTerm.StartDate;
		if (imperatorRulerTerm.Government is not null) {
			Government = governmentMapper.GetCK3GovernmentForImperatorGovernment(
				irGovernmentId: imperatorRulerTerm.Government, 
				rank: ck3Title.Rank, 
				irCultureId: ck3Title.ImperatorCountry?.PrimaryCulture,
				enabledCK3Dlcs);
		}

		var preImperatorRuler = imperatorRulerTerm.PreImperatorRuler;
		if (preImperatorRuler?.BirthDate is null) {
			return;
		}
		if (preImperatorRuler.DeathDate is null) {
			return;
		}
		if (preImperatorRuler.Country is not null) {
			// create a new ruler character
			var character = new Character(
				preImperatorRuler,
				StartDate,
				preImperatorRuler.Country,
				characters,
				irLocDB,
				ck3LocDB,
				religionMapper,
				cultureMapper,
				nicknameMapper,
				provinceMapper,
				config
			);
			if (characters.ContainsKey(character.Id)) {
				Logger.Warn($"Cannot add pre-Imperator ruler {character.Id} " +
				            "- a character with this ID already exists!");
				return;
			}
			characters.Add(character);
			CharacterId = character.Id;
		}
	}
}