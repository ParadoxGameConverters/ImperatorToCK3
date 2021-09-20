using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.CK3.Wars {
	public class War {
		public Date StartDate { get; } = new(1, 1, 1);
		public Date EndDate { get; }
		public List<string> TargetedTitles { get; } = new();
		public string? CasusBelli { get; }
		public List<string> Attackers { get; } = new();
		public List<string> Defenders { get; } = new();
		public string Claimant { get; }

		public War(Imperator.Diplomacy.War impWar, Imperator.Countries.Countries impCountries, Mappers.War.WarMapper warMapper, Date ck3BookmarkDate) {
			StartDate = new Date(impWar.StartDate);
			if (StartDate.Year < 0) {
				StartDate = new Date(1, 1, 1);
			}
			EndDate = new Date(ck3BookmarkDate);
			EndDate.ChangeByDays(1);

			foreach (var countryId in impWar.AttackerCountryIds) {
				var impCountry = impCountries.StoredCountries[countryId];
				var ck3Title = impCountry.CK3Title;
				if (ck3Title is not null) {
					var ck3RulerId = ck3Title.GetHolderId(ck3BookmarkDate);
					if (ck3RulerId != "0") {
						Attackers.Add(ck3RulerId);
					}
				}
			}
			Claimant = Attackers[0];
			foreach (var countryId in impWar.DefenderCountryIds) {
				var impCountry = impCountries.StoredCountries[countryId];
				var ck3Title = impCountry.CK3Title;
				if (ck3Title is not null) {
					var ck3RulerId = ck3Title.GetHolderId(ck3BookmarkDate);
					if (ck3RulerId != "0") {
						if (Defenders.Count == 0) {
							TargetedTitles.Add(ck3Title.Name);// this is a dev workaround, TODO: replace TargetedTitles setting with properly determined CK3 title
						}
						Defenders.Add(ck3RulerId);
					}
				}
			}

			CasusBelli = warMapper.GetCK3CBForImperatorWarGoal(impWar.WarGoal);
		}
	}
}
