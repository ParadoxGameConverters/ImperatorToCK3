using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Provinces {
	public class Provinces : Parser {
		public Dictionary<ulong, Province> StoredProvinces { get; } = new();

		public Provinces() { }
		public Provinces(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterRegex(CommonRegexes.Integer, (reader, provIdStr) => {
				var newProvince = Province.Parse(reader, ulong.Parse(provIdStr));
				StoredProvinces.Add(newProvince.Id, newProvince);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public void LinkPops(Pops.Pops pops) {
			var counter = StoredProvinces.Values.Sum(province => province.LinkPops(pops));
			Logger.Info($"{counter} pops linked to provinces.");
		}
		public void LinkCountries(Countries.Countries countries) {
			var counter = 0;
			foreach (var (provId, province) in StoredProvinces) {
				if (countries.StoredCountries.TryGetValue(province.OwnerCountry.Key, out var countryToLink)) {
					// link both ways
					province.LinkOwnerCountry(countryToLink);
					countryToLink.RegisterProvince(province);
					++counter;
				} else {
					Logger.Warn($"Country with ID {province.OwnerCountry.Key} has no definition!");
				}
			}
			Logger.Info($"{counter} countries linked to provinces.");
		}
	}
}
