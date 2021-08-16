using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.Mappers.Nickname {
	public class NicknameMapper : Parser {
		private Dictionary<string, string> impToCK3NicknameMap = new();

		public NicknameMapper() {
			Logger.Log(LogLevel.Info, "Parsing nickname mappings.");
			RegisterKeys();
			ParseFile("configurables/nickname_map.txt");
			ClearRegisteredRules();
			Logger.Log(LogLevel.Info, "Loaded " + impToCK3NicknameMap.Count + " nickname links.");
		}
		public NicknameMapper(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("link", (reader) => {
				var mapping = new NicknameMapping(reader);
				if (mapping.Ck3Nickname is not null) {
					foreach (var imperatorNickname in mapping.ImperatorNicknames) {
						impToCK3NicknameMap.Add(imperatorNickname, mapping.Ck3Nickname);
					}
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public string? GetCK3NicknameForImperatorNickname(string impNickname) {
			var gotValue = impToCK3NicknameMap.TryGetValue(impNickname, out var ck3Nickname);
			if (gotValue) {
				return ck3Nickname;
			}
			return null;
		}
	}
}
