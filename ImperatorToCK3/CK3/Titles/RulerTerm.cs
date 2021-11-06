using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Titles {
	public class RulerTerm {
		public string CharacterId { get; } = "0";
		public Date StartDate { get; }
		public string? Government { get; }
		public Imperator.Countries.RulerTerm.PreImperatorRulerInfo? PreImperatorRuler { get; }

		public RulerTerm(
			Imperator.Countries.RulerTerm imperatorRulerTerm,
			IDictionary<string, Character> charactersDict,
			GovernmentMapper governmentMapper,
			LocalizationMapper localizationMapper,
			ReligionMapper religionMapper,
			CultureMapper cultureMapper,
			NicknameMapper nicknameMapper,
			ProvinceMapper provinceMapper
		) {
			if (imperatorRulerTerm.CharacterId is not null) {
				CharacterId = $"imperator{imperatorRulerTerm.CharacterId}";
			}
			StartDate = imperatorRulerTerm.StartDate;
			if (imperatorRulerTerm.Government is not null) {
				Government = governmentMapper.GetCK3GovernmentForImperatorGovernment(imperatorRulerTerm.Government);
			}

			PreImperatorRuler = imperatorRulerTerm.PreImperatorRuler;
			if (PreImperatorRuler?.Country is not null) {
				// create a new ruler character
				var character = new Characters.Character(
					PreImperatorRuler,
					StartDate,
					PreImperatorRuler.Country,
					localizationMapper,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					provinceMapper
				);
				charactersDict.Add(character.Id, character);
				CharacterId = character.Id;
			}
		}
	}
}
