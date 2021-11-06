using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Nickname {
	public class NicknameMapper : Parser {
		private Dictionary<string, string> impToCK3NicknameMap = new();

		public NicknameMapper(string filePath) {
			Logger.Info("Parsing nickname mappings.");
			RegisterKeys();
			ParseFile(filePath);
			ClearRegisteredRules();
			Logger.Info($"Loaded {impToCK3NicknameMap.Count} nickname links.");
		}
		public NicknameMapper(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("link", reader => {
				var mapping = new NicknameMapping(reader);
				if (mapping.CK3Nickname is null) {
					return;
				}

				foreach (var imperatorNickname in mapping.ImperatorNicknames) {
					impToCK3NicknameMap.Add(imperatorNickname, mapping.CK3Nickname);
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public string? GetCK3NicknameForImperatorNickname(string? impNickname) {
			if (impNickname is null) {
				return null;
			}
			return impToCK3NicknameMap.TryGetValue(impNickname, out var ck3Nickname) ? ck3Nickname : null;
		}
	}
}
