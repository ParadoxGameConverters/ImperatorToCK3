using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Provinces;

public partial class Province {
	public static HashSet<string> IgnoredTokens { get; } = new();
	static Province() {
		provinceParser.RegisterKeyword("province_name", reader =>
			parsedProvince.Name = new ProvinceName(reader).Name
		);
		provinceParser.RegisterKeyword("culture", reader =>
			parsedProvince.Culture = reader.GetString()
		);
		provinceParser.RegisterKeyword("religion", reader =>
			parsedProvince.Religion = reader.GetString()
		);
		provinceParser.RegisterKeyword("owner", reader =>
			parsedProvince.parsedOwnerCountryId = reader.GetULong()
		);
		provinceParser.RegisterKeyword("controller", reader =>
			parsedProvince.Controller = reader.GetULong()
		);
		provinceParser.RegisterKeyword("pop", reader =>
			parsedProvince.parsedPopIds.Add(reader.GetULong())
		);
		provinceParser.RegisterKeyword("civilization_value", reader =>
			parsedProvince.CivilizationValue = reader.GetDouble()
		);
		provinceParser.RegisterKeyword("province_rank", reader => {
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
		});
		provinceParser.RegisterKeyword("fort", reader =>
			parsedProvince.Fort = reader.GetPDXBool()
		);
		provinceParser.RegisterKeyword("holy_site", reader => {
			var deityId = reader.GetULong();
			// 4294967295 equals (2^32 − 1) and is the default value
			// otherwise, the value is the ID of a deity (see deities_database block in the save)
			if (deityId != 4294967295) {
				parsedProvince.HolySiteDeityId = deityId;
			}
		});
		provinceParser.RegisterKeyword("buildings", reader => {
			var buildingsList = reader.GetInts();
			parsedProvince.BuildingCount = (uint)buildingsList.Sum();
		});
		provinceParser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
			IgnoredTokens.Add(token);
			ParserHelpers.IgnoreItem(reader);
		});
	}
	public static Province Parse(BufferedReader reader, ulong provinceId) {
		parsedProvince = new Province(provinceId);
		provinceParser.ParseStream(reader);
		return parsedProvince;
	}

	private static Province parsedProvince = new(0);
	private static readonly Parser provinceParser = new();
}