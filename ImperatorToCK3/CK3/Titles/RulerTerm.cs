using commonItems;
using commonItems.Localization;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;

namespace ImperatorToCK3.CK3.Titles;

public sealed class RulerTerm {
	public string? CharacterId { get; }
	public Date StartDate { get; }
	public string? Government { get; }
	public Imperator.Countries.RulerTerm.PreImperatorRulerInfo? PreImperatorRuler { get; }

	public RulerTerm(
		Imperator.Countries.RulerTerm imperatorRulerTerm,
		Characters.CharacterCollection characters,
		GovernmentMapper governmentMapper,
		LocDB irLocDB,
		ReligionMapper religionMapper,
		CultureMapper cultureMapper,
		NicknameMapper nicknameMapper,
		ProvinceMapper provinceMapper,
		Configuration config
	) {
		if (imperatorRulerTerm.CharacterId is not null) {
			CharacterId = $"imperator{imperatorRulerTerm.CharacterId}";
		}
		StartDate = imperatorRulerTerm.StartDate;
		if (imperatorRulerTerm.Government is not null) {
			Government = governmentMapper.GetCK3GovernmentForImperatorGovernment(imperatorRulerTerm.Government, null);
		}

		PreImperatorRuler = imperatorRulerTerm.PreImperatorRuler;
		if (PreImperatorRuler?.BirthDate is null) {
			return;
		}
		if (PreImperatorRuler.DeathDate is null) {
			return;
		}
		if (PreImperatorRuler.Country is not null) {
			// create a new ruler character
			var character = new Character(
				PreImperatorRuler,
				StartDate,
				PreImperatorRuler.Country,
				characters,
				irLocDB,
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