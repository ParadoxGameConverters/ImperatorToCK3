using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.Nickname {
	public class NicknameMapping {
		public SortedSet<string> ImperatorNicknames { get; private set; } = new();
		public string? Ck3Nickname { get; private set; } = "";
		
		public NicknameMapping(BufferedReader reader) {
			var parser = new Parser();
			parser.RegisterKeyword("ck3", (reader) => {
				Ck3Nickname = new SingleString(reader).String;
			});
			parser.RegisterKeyword("ck3", (reader) => {
				ImperatorNicknames.Add(new SingleString(reader).String);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseStream(reader);
			parser.ClearRegisteredRules();
		}
	}
}
