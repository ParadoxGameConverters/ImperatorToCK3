using commonItems;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.States;
using System.Linq;

namespace ImperatorToCK3.Imperator.Provinces;

public partial class Province {
	public static IgnoredKeywordsSet IgnoredTokens { get; } = new();
	static Province() {
		provinceParser.RegisterKeyword("province_name", reader =>
			parsedProvince.Name = new ProvinceName(reader).Name
		);
		provinceParser.RegisterKeyword("state", reader => parsedStateId = reader.GetULong());
		provinceParser.RegisterKeyword("owner", reader => parsedOwnerId = reader.GetULong());
		provinceParser.RegisterKeyword("controller", reader =>
			parsedProvince.Controller = reader.GetULong()
		);
		provinceParser.RegisterKeyword("culture", reader =>
			parsedProvince.Culture = reader.GetString()
		);
		provinceParser.RegisterKeyword("religion", reader =>
			parsedProvince.ReligionId = reader.GetString()
		);
		provinceParser.RegisterKeyword("pop", reader =>
			parsedProvince.parsedPopIds.Add(reader.GetULong())
		);
		provinceParser.RegisterKeyword("civilization_value", reader =>
			parsedProvince.CivilizationValue = reader.GetDouble()
		);
		provinceParser.RegisterKeyword("province_rank", SetParsedProvinceRank());
		provinceParser.RegisterKeyword("fort", reader =>
			parsedProvince.Fort = reader.GetBool()
		);
		provinceParser.RegisterKeyword("holdings", reader => {
			var holdingOwnerId = reader.GetULong();
			// 4294967295 equals (2^32 − 1) and is the default value
			// otherwise, the value is the ID of a character
			if (holdingOwnerId != 4294967295) {
				parsedProvince.HoldingOwnerId = holdingOwnerId;
			}
		});
		provinceParser.RegisterKeyword("holy_site", reader => {
			var holySiteId = reader.GetULong();
			// 4294967295 equals (2^32 − 1) and is the default value
			// otherwise, the value is the ID of a deity (see deities_database block in the save)
			if (holySiteId != 4294967295) {
				parsedProvince.HolySiteId = holySiteId;
			}
		});
		provinceParser.RegisterKeyword("buildings", reader => {
			var buildingsList = reader.GetInts();
			parsedProvince.BuildingCount = (uint)buildingsList.Sum();
		});
		provinceParser.IgnoreAndStoreUnregisteredItems(IgnoredTokens);
	}

	private static SimpleDel SetParsedProvinceRank() {
		return reader => {
			var provinceRankStr = reader.GetString();
			switch (provinceRankStr) {
				case "settlement":
					parsedProvince.ProvinceRank = ProvinceRank.settlement;
					break;
				case "city":
					parsedProvince.ProvinceRank = ProvinceRank.city;
					break;
				case "city_metropolis":
					parsedProvince.ProvinceRank = ProvinceRank.city_metropolis;
					break;
				default:
					Logger.Warn($"Unknown province rank for province {parsedProvince.Id}: {provinceRankStr}");
					break;
			}
		};
	}

	public static Province Parse(BufferedReader reader, ulong provinceId, StateCollection states, CountryCollection countries) {
		parsedStateId = null;
		parsedOwnerId = null;

		parsedProvince = new Province(provinceId);
		provinceParser.ParseStream(reader);

		if (parsedStateId is not null) {
			if (!states.TryGetValue(parsedStateId.Value, out var state)) {
				Logger.Warn($"Province {parsedProvince.Id} has state ID {parsedStateId}, but no such state has been loaded!");
			} else {
				parsedProvince.State = state;
			}
		}

		parsedProvince.TryLinkOwnerCountry(parsedOwnerId, countries);

		return parsedProvince;
	}

	private void TryLinkOwnerCountry(ulong? countryId, CountryCollection countries) {
		if (countryId is null) {
			return;
		}
		if (countries.TryGetValue(countryId.Value, out var countryToLink)) {
			// link both ways
			OwnerCountry = countryToLink;
			countryToLink.RegisterProvince(this);
			return;
		}

		Logger.Warn($"Country with ID {countryId} has no definition!");
	}

	private static Province parsedProvince = new(0);
	private static ulong? parsedStateId = null;
	private static ulong? parsedOwnerId = null;

	private static readonly Parser provinceParser = new();
}