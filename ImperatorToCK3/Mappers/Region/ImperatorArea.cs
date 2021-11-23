using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Region {
	public class ImperatorArea : Parser {
		public SortedSet<ulong> Provinces { get; } = new();

		public ImperatorArea(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("provinces", reader => {
				foreach (var id in reader.GetULongs()) {
					Provinces.Add(id);
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public bool ContainsProvince(ulong province) {
			return Provinces.Contains(province);
		}
	}
}