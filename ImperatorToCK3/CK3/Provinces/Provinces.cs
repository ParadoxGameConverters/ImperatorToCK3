using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Provinces {
	public class Provinces : Parser {
		public Provinces() { }
		public Provinces(string filePath, Date ck3BookmarkDate) {
			RegisterKeys(ck3BookmarkDate);
			ParseFile(filePath);
			ClearRegisteredRules();
		}
		public Dictionary<ulong, Province> StoredProvinces { get; } = new();

		private void RegisterKeys(Date ck3BookmarkDate) {
			RegisterRegex(CommonRegexes.Integer, (reader, provinceIdString) => {
				var provinceId = ulong.Parse(provinceIdString);
				var newProvince = new Province(provinceId, reader, ck3BookmarkDate);
				StoredProvinces.Add(provinceId, newProvince);
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
