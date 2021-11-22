using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Provinces {
	public class Provinces : Dictionary<ulong, Province> {
		public Provinces() { }
		public Provinces(string filePath, Date ck3BookmarkDate) {
			var parser = new Parser();
			RegisterKeys(parser, ck3BookmarkDate);
			parser.ParseFile(filePath);
		}

		public void Add(Province newProvince) {
			Add(newProvince.Id, newProvince);
		}

		private void RegisterKeys(Parser parser, Date ck3BookmarkDate) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, provinceIdString) => {
				var provinceId = ulong.Parse(provinceIdString);
				var newProvince = new Province(provinceId, reader, ck3BookmarkDate);
				Add(newProvince);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
