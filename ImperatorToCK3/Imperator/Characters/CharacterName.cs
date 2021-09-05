using commonItems;

namespace ImperatorToCK3.Imperator.Characters {
	public class CharacterName : Parser {
		private string name = string.Empty;
		private string? customName;
		public string Name {
			get {
				return customName ?? name;
			}
		}

		public CharacterName(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("name", reader => name = ParserHelpers.GetString(reader));
			RegisterKeyword("custom_name", reader => customName = ParserHelpers.GetString(reader));
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
