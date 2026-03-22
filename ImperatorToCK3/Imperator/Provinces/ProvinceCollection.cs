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
		var parser = new Parser(implicitVariableHandling: false);
		parser.RegisterRegex(CommonRegexes.Integer, (reader, provIdStr) => {
			var newProvince = Province.Parse(reader, ulong.Parse(provIdStr), states, countries);
			Add(newProvince);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(provincesReader);
		
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
			var province = this[provinceId];
			province.OwnerCountry = country;
			country.RegisterProvince(province);
		}
	}
	
	private Country? GetCountryForColorableImpassable(ulong provinceId, MapData irMapData) {
		var neighborProvIds = irMapData.GetNeighborProvinceIds(provinceId);
		int neighborsCount = neighborProvIds.Count;

		// Count neighboring owners and track the strongest candidate.
		Dictionary<Country, int> ownerCounts = [];
		Country? ownerCandidate = null;
		int ownerCandidateCount = 0;
		foreach (var neighborProvId in neighborProvIds) {
			if (!TryGetValue(neighborProvId, out var neighborProvince) || neighborProvince.OwnerCountry is null) {
				continue;
			}

			if (!ownerCounts.TryAdd(neighborProvince.OwnerCountry, 1)) {
				ownerCounts[neighborProvince.OwnerCountry]++;
			}

			var count = ownerCounts[neighborProvince.OwnerCountry];
			if (count > ownerCandidateCount) {
				ownerCandidate = neighborProvince.OwnerCountry;
				ownerCandidateCount = count;
			}
		}

		// If any country controls at least half of the neighboring provinces, the impassable should be colored.
		if (ownerCandidate is not null && ownerCandidateCount >= (float)neighborsCount / 2) {
			return ownerCandidate;
		}
		return null;
	}
}