using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Pops;
using ImperatorToCK3.Imperator.Religions;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Provinces;

public enum ProvinceRank { settlement, city, city_metropolis }
public partial class Province : IIdentifiable<ulong> {
	public ulong Id { get; } = 0;
	public string Name { get; set; } = "";
	public string Culture { get; set; } = "";
	public string Religion { get; set; } = "";
	private ulong? parsedOwnerCountryId;
	public Country? OwnerCountry { get; set; }
	public ulong Controller { get; set; } = 0;
	public Dictionary<ulong, Pop> Pops { get; set; } = new();
	public ProvinceRank ProvinceRank { get; set; } = ProvinceRank.settlement;
	public PDXBool Fort { get; set; } = new(false);
	public bool IsHolySite => HolySiteId is not null;
	public ulong? HolySiteId { get; set; } = null;
	public uint BuildingCount { get; set; } = 0;
	public double CivilizationValue { get; set; } = 0;

	public Province(ulong id) {
		Id = id;
	}

	public int GetPopCount() {
		return Pops.Count;
	}

	public Religion? GetReligion(ReligionCollection religions) {
		return religions.TryGetValue(Religion, out var religion) ? religion : null;
	}

	public Deity? GetHolySiteDeity(ReligionCollection religions) {
		return HolySiteId is null ? null : religions.GetDeityForHolySiteId((ulong)HolySiteId);
	}

	public void LinkOwnerCountry(Country country) {
		if (parsedOwnerCountryId is not null && parsedOwnerCountryId != country.Id) {
			Logger.Warn($"Province {Id}: linking owner {country.Id} that doesn't match owner from save ({parsedOwnerCountryId})!");
		}

		OwnerCountry = country;
	}

	public bool TryLinkOwnerCountry(CountryCollection countries) {
		if (parsedOwnerCountryId is null) {
			return false;
		}
		if (countries.TryGetValue((ulong)parsedOwnerCountryId, out var countryToLink)) {
			// link both ways
			LinkOwnerCountry(countryToLink);
			countryToLink.RegisterProvince(this);
			return true;
		}

		Logger.Warn($"Country with ID {parsedOwnerCountryId} has no definition!");
		return false;
	}

	// Returns a count of linked pops
	public int LinkPops(PopCollection pops) {
		int counter = 0;
		foreach (var popId in parsedPopIds) {
			if (pops.TryGetValue(popId, out var popToLink)) {
				Pops.Add(popId, popToLink);
				++counter;
			} else {
				Logger.Warn($"Pop with ID {popId} has no definition!");
			}
		}
		return counter;
	}

	private readonly HashSet<ulong> parsedPopIds = new();
}