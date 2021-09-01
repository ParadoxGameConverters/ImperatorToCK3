using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.CK3.Provinces {
	public class Provinces : Parser {
		public Provinces() { }
		public Provinces(string filePath) {
			RegisterKeys();
			ParseFile(filePath);
			ClearRegisteredRules();
		}
		public Dictionary<ulong, Province> StoredProvinces { get; } = new();

		private void RegisterKeys() {
			RegisterRegex(CommonRegexes.Integer, (reader, provinceIdString) => {
				var provID = ulong.Parse(provinceIdString);
				var newProvince = new Province(provID, reader);
				StoredProvinces.Add(provID, newProvince);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
