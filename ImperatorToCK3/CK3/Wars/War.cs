using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Exceptions;
using ImperatorToCK3.Imperator.States;
using ImperatorToCK3.Mappers.Province;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Wars; 

public class War {
	public Date StartDate { get; } = "2.1.1";
	public Date EndDate { get; }
	public OrderedSet<string> TargetedTitles { get; } = new();
	public string? CasusBelli { get; }
	public List<string> Attackers { get; } = new();
	public List<string> Defenders { get; } = new();
	public string Claimant { get; }

	public War(Imperator.Diplomacy.War irWar, Mappers.War.WarMapper warMapper, ProvinceMapper provinceMapper, Imperator.Countries.CountryCollection impCountries, StateCollection irStates, ProvinceCollection ck3Provinces, Title.LandedTitles titles, Date ck3BookmarkDate) {
		StartDate = new Date(irWar.StartDate);
		if (StartDate.Year < 2) {
			StartDate = new Date(2, 1, 1);
		}
		EndDate = new Date(ck3BookmarkDate).ChangeByDays(1);

		foreach (var countryId in irWar.AttackerCountryIds) {
			var impCountry = impCountries[countryId];
			var ck3Title = impCountry.CK3Title;
			if (ck3Title is not null) {
				var ck3RulerId = ck3Title.GetHolderId(ck3BookmarkDate);
				if (ck3RulerId != "0") {
					Attackers.Add(ck3RulerId);
				}
			}
		}

		if (!Attackers.Any()) {
			throw new ConverterException("War has no valid attackers!");
		}
		Claimant = Attackers[0];

		if (irWar.TargetedStateId is not null) {
			var state = irStates[irWar.TargetedStateId.Value];
			var targetedCountyIds = state.Provinces
				.SelectMany(p => provinceMapper.GetCK3ProvinceNumbers(p.Id))
				.Select(titles.GetCountyForProvince)
				.Where(t => t is not null)
				.Cast<Title>()
				.Select(t => t.Id)
				.ToHashSet();
			TargetedTitles.UnionWith(targetedCountyIds);
		}
		
		foreach (var countryId in irWar.DefenderCountryIds) {
			var impCountry = impCountries[countryId];
			var ck3Title = impCountry.CK3Title;
			if (ck3Title is not null) {
				var ck3RulerId = ck3Title.GetHolderId(ck3BookmarkDate);
				if (ck3RulerId == "0") {
					continue;
				}

				if (Defenders.Count == 0) { // we're adding the first defender
					if (!TargetedTitles.Any()) {
						TargetedTitles.Add(ck3Title.CapitalCountyId ?? ck3Title.Id); // TODO: replace TargetedTitles setting with properly determined CK3 title
					}
				}
				Defenders.Add(ck3RulerId);
			}
		}

		CasusBelli = warMapper.GetCK3CBForImperatorWarGoal(irWar.WarGoal!);
	}
}