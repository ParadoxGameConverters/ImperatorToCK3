using System.Linq;
using commonItems;

namespace ImperatorToCK3.Imperator.Provinces {
	public class ProvinceFactory : Parser {
		private Province province = new(0);
		public ProvinceFactory() {
			RegisterKeyword("province_name", reader => {
				province.Name = new ProvinceName(reader).Name;
			});
			RegisterKeyword("culture", reader => {
				province.Culture = new SingleString(reader).String;
			});
			RegisterKeyword("religion", reader => {
				province.Religion = new SingleString(reader).String;
			});
			RegisterKeyword("owner", reader => {
				province.OwnerCountry = new(new SingleULong(reader).ULong, null);
			});
			RegisterKeyword("controller", reader => {
				province.Controller = new SingleULong(reader).ULong;
			});
			RegisterKeyword("pop", reader => {
				province.Pops.Add(new SingleULong(reader).ULong, null);
			});
			RegisterKeyword("civilization_value", reader => {
				province.CivilizationValue = new SingleDouble(reader).Double;
			});
			RegisterKeyword("province_rank", reader => {
				var provinceRankStr = new SingleString(reader).String;
				if (provinceRankStr == "settlement")
					province.ProvinceRank = ProvinceRank.settlement;
				else if (provinceRankStr == "city")
					province.ProvinceRank = ProvinceRank.city;
				else if (provinceRankStr == "city_metropolis")
					province.ProvinceRank = ProvinceRank.city_metropolis;
				else
					Logger.Log(LogLevel.Warning, $"Unknown province rank for province {province.ID}: {provinceRankStr}");
			});
			RegisterKeyword("fort", reader => {
				province.Fort = new SingleString(reader).String == "yes";
			});
			RegisterKeyword("holy_site", reader => {
				province.HolySite = new SingleULong(reader).ULong != 4294967295; // 4294967295 is 2^32 − 1 and is the default value
			});
			RegisterKeyword("buildings", reader => {
				var buildingsList = new IntList(reader).Ints;
				province.BuildingCount = (uint)buildingsList.Sum();
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public Province? GetProvince(BufferedReader reader, ulong provinceID) {
			province = new Province(provinceID);
			ParseStream(reader);
			return province;
		}
	}
}
