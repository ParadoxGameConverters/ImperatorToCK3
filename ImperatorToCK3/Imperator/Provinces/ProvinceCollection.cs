using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Pops;
using System.Linq;

namespace ImperatorToCK3.Imperator.Provinces {
	public class ProvinceCollection : IdObjectCollection<ulong, Province> {
		public ProvinceCollection() { }
		public ProvinceCollection(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);
		}
		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, provIdStr) => {
				var newProvince = Province.Parse(reader, ulong.Parse(provIdStr));
				Add(newProvince);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public void LinkPops(PopCollection pops) {
			var counter = this.Sum(province => province.LinkPops(pops));
			Logger.Info($"{counter} pops linked to provinces.");
		}
		public void LinkCountries(CountryCollection countries) {
			var counter = this.Count(province => province.TryLinkOwnerCounty(countries));
			Logger.Info($"{counter} provinces linked to countries.");
		}
	}
}
