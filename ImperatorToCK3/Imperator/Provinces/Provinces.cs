using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.Imperator.Provinces {
	public class Provinces : Parser {
		public Dictionary<ulong, Province?> StoredProvinces { get; } = new();

		public Provinces(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterRegex(CommonRegexes.Integer, (reader, provIdStr) => {
				var newProvince = Province.Parse(reader, ulong.Parse(provIdStr));
				StoredProvinces.Add(newProvince.ID, newProvince);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public void LinkPops(Pops.Pops pops) {
			var counter = 0;
			foreach (var (provId, province) in StoredProvinces) {
				if (province is null) {
					Logger.Warn($"Not linking pops to null province {provId}");
					continue;
				}
				if (province.GetPopCount() > 0) {
					var newPops = new Dictionary<ulong, Pops.Pop?>();
					foreach (var popId in province.Pops.Keys) {
						if (pops.StoredPops.TryGetValue(popId, out var popToLink)) {
							newPops.Add(popId, popToLink);
							++counter;
						} else {
							Logger.Warn($"Pop with ID {popId} has no definition!");
						}
					}
					province.Pops = newPops;
				}
			}
			Logger.Info($"{counter} pops linked to provinces.");
		}
		public void LinkCountries(TempMocks.Countries.Countries countries) {
			var counter = 0;
			foreach(var (provId, province) in StoredProvinces) {
				if (province is null) {
					Logger.Warn($"Not linking country to null province {provId}");
					continue;
				}
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
