using commonItems;
using ImperatorToCK3.Mappers.Government;

namespace ImperatorToCK3.CK3.Titles {
	public class RulerTerm {
		public string CharacterId { get; private set; } = "0";
		public Date StartDate { get; private set; }
		public string? Government { get; private set; }

		public RulerTerm(Imperator.Countries.RulerTerm imperatorRulerTerm, GovernmentMapper governmentMapper) {
			if (imperatorRulerTerm.CharacterId is not null) {
				CharacterId = "imperator" + imperatorRulerTerm.CharacterId.ToString();
			}
			StartDate = imperatorRulerTerm.StartDate;
			if (imperatorRulerTerm.Government is not null) {
				Government = governmentMapper.GetCK3GovernmentForImperatorGovernment(imperatorRulerTerm.Government);
			}
		}
	}
}
