using commonItems;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ImperatorToCK3.Imperator.Provinces {
	public class Provinces : IReadOnlyDictionary<ulong, Province> {
		public Provinces() { }
		public Provinces(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);
		}
		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, provIdStr) => {
				var newProvince = Province.Parse(reader, ulong.Parse(provIdStr));
				Add(newProvince.Id, newProvince);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public void LinkPops(Pops.Pops pops) {
			var counter = Values.Sum(province => province.LinkPops(pops));
			Logger.Info($"{counter} pops linked to provinces.");
		}
		public void LinkCountries(Countries.Countries countries) {
			var counter = 0;
			foreach (var province in Values) {
				if (countries.TryGetValue(province.OwnerCountry.Key, out var countryToLink)) {
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

		public bool ContainsKey(ulong key) => provincesDict.ContainsKey(key);
		public bool TryGetValue(ulong key, [MaybeNullWhen(false)] out Province value) => provincesDict.TryGetValue(key, out value);
		public IEnumerator<KeyValuePair<ulong, Province>> GetEnumerator() => provincesDict.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => provincesDict.GetEnumerator();
		public IEnumerable<ulong> Keys => provincesDict.Keys;
		public IEnumerable<Province> Values => provincesDict.Values;
		public int Count => provincesDict.Count;
		public Province this[ulong key] => provincesDict[key];
		private readonly Dictionary<ulong, Province> provincesDict = new();
	}
}
