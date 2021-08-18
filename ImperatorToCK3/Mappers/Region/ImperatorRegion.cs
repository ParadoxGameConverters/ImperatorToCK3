using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.Region {
	public class ImperatorRegion : Parser {
		public SortedDictionary<string, ImperatorArea?> Areas { get; } = new();

		public ImperatorRegion(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("areas", reader => {
				foreach (var name in new StringList(reader).Strings) {
					Areas.Add(name, null);
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public bool RegionContainsProvince(ulong province) {
			foreach (var (_, area) in Areas) {
				if (area is not null && area.ContainsProvince(province)) {
					return true;
				}
			}
			return false;
		}
		public void LinkArea(string name, ImperatorArea area) {
			Areas[name] = area;
		}
	}
}
