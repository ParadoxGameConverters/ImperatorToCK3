using commonItems;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ImperatorToCK3.CK3.Provinces {
	public class Provinces : IReadOnlyDictionary<ulong, Province> {
		public Provinces() { }
		public Provinces(string filePath, Date ck3BookmarkDate) {
			var parser = new Parser();
			RegisterKeys(parser, ck3BookmarkDate);
			parser.ParseFile(filePath);
		}

		public void Add(Province newProvince) {
			provincesDict.Add(newProvince.Id, newProvince);
		}

		private void RegisterKeys(Parser parser, Date ck3BookmarkDate) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, provinceIdString) => {
				var provinceId = ulong.Parse(provinceIdString);
				var newProvince = new Province(provinceId, reader, ck3BookmarkDate);
				Add(newProvince);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
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
