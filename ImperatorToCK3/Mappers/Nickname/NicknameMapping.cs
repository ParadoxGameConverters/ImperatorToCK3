using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Nickname {
	public class NicknameMapping {
		public SortedSet<string> ImperatorNicknames { get; private set; } = new();
		public string? CK3Nickname { get; private set; }

		public NicknameMapping(BufferedReader reader) {
			var parser = new Parser();
			parser.RegisterKeyword("ck3", reader => {
				CK3Nickname = reader.GetString();
			});
			parser.RegisterKeyword("imp", reader => {
				ImperatorNicknames.Add(reader.GetString());
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseStream(reader);
			parser.ClearRegisteredRules();
		}
	}
}
