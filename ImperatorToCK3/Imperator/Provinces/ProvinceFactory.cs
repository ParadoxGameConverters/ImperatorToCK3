using System.Linq;
using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Imperator.Provinces {
	public partial class Province {
		private static Province province = new(0);
		private static readonly Parser provinceParser = new();
		public static HashSet<string> IgnoredTokens { get; } = new();
		static Province() {
			provinceParser.RegisterKeyword("province_name", reader =>
				province.Name = new ProvinceName(reader).Name
			);
			provinceParser.RegisterKeyword("culture", reader =>
				province.Culture = new SingleString(reader).String
			);
			provinceParser.RegisterKeyword("religion", reader =>
				province.Religion = new SingleString(reader).String
			);
			provinceParser.RegisterKeyword("owner", reader =>
				province.OwnerCountry = new(new SingleULong(reader).ULong, null)
			);
			provinceParser.RegisterKeyword("controller", reader =>
				province.Controller = new SingleULong(reader).ULong
			);
			provinceParser.RegisterKeyword("pop", reader =>
				province.Pops.Add(new SingleULong(reader).ULong, null)
			);
			provinceParser.RegisterKeyword("civilization_value", reader =>
				province.CivilizationValue = new SingleDouble(reader).Double
			);
			provinceParser.RegisterKeyword("province_rank", reader => {
				var provinceRankStr = new SingleString(reader).String;
				switch (provinceRankStr) {
					case "settlement":
						province.ProvinceRank = ProvinceRank.settlement;
						break;
					case "city":
						province.ProvinceRank = ProvinceRank.city;
						break;
					case "city_metropolis":
						province.ProvinceRank = ProvinceRank.city_metropolis;
						break;
					default:
						Logger.Warn($"Unknown province rank for province {province.ID}: {provinceRankStr}");
						break;
				}
			});
			provinceParser.RegisterKeyword("fort", reader =>
				province.Fort = new SingleString(reader).String == "yes"
				);
			provinceParser.RegisterKeyword("holy_site", reader => {
				// 4294967295 is 2^32 − 1 and is the default value
				province.HolySite = new SingleULong(reader).ULong != 4294967295;
			});
			provinceParser.RegisterKeyword("buildings", reader => {
				var buildingsList = new IntList(reader).Ints;
				province.BuildingCount = (uint)buildingsList.Sum();
			});
			provinceParser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				IgnoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});
		}
		public static Province Parse(BufferedReader reader, ulong provinceID) {
			province = new Province(provinceID);
			provinceParser.ParseStream(reader);
			return province;
		}
	}
}
