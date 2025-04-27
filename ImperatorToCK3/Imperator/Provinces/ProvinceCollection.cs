using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Pops;
using ImperatorToCK3.Imperator.States;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Provinces;

internal sealed class ProvinceCollection : IdObjectCollection<ulong, Province> {
	public void LoadProvinces(BufferedReader provincesReader, StateCollection states, CountryCollection countries, MapData irMapData) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, provIdStr) => {
			var newProvince = Province.Parse(reader, ulong.Parse(provIdStr), states, countries);
			Add(newProvince);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(provincesReader);
		
		
		// TODO: for impassables that are colored after the neighboring I:R countries, treat them as owned by the country
		
		// After all the provinces are loaded, we can determine if there are impassables to be considered owned.
		// This should match the impassables colored with a country color on the Imperator map.
		DetermineImpassableOwnership(irMapData);
	}
	public void LinkPops(PopCollection pops) {
		var counter = this.Sum(province => province.LinkPops(pops));
		Logger.Info($"{counter} pops linked to provinces.");
	}
	
	private void DetermineImpassableOwnership(MapData irMapData) {
		// Store the map of province -> country to be assigned in a dict, to avoid one impassable being given an owner
		// skewing the calculation for the neighboring impassables.
		Dictionary<ulong, Country> impassableOwnership = [];
		
		foreach (var province in this) {
			if (province.OwnerCountry is not null) {
				continue;
			}

			if (!irMapData.IsColorableImpassable(province.Id)) {
				continue;
			}

			Country? country = GetCountryForColorableImpassable(province.Id, irMapData);
			if (country is null) {
				continue;
			}
				
			impassableOwnership[province.Id] = country;
		}

		foreach (var (provinceId, country) in impassableOwnership) {
			Logger.Notice($"Assigning impassable province {provinceId} to country {country.Name}"); // TODO: remove this
			var province = this[provinceId];
			province.OwnerCountry = country;
			country.RegisterProvince(province);
		}
	}
	
	private Country? GetCountryForColorableImpassable(ulong provinceId, MapData irMapData) {
		var neighborProvIds = irMapData.GetNeighborProvinceIds(provinceId);
		int neighborsCount = neighborProvIds.Count;

		// Group the neighboring provinces by their owner. The one with most owned neighbors may be the owner of the impassable.
		var ownerCandidate = neighborProvIds
			.Select(provId => this[provId].OwnerCountry)
			.Where(country => country is not null)
			.GroupBy(country => country)
			.OrderByDescending(group => group.Count())
			.FirstOrDefault();

		// If any country controls at least half of the neighboring provinces, the impassable should be colored.
		if (ownerCandidate is not null && ownerCandidate.Count() >= (float)neighborsCount / 2) {
			return ownerCandidate.Key;
		}
		return null;
	}
}