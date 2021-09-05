using commonItems;

namespace ImperatorToCK3.Imperator.Characters {
	public class CharacterName : Parser {
		public string Name { get; private set; } = string.Empty; // key for localization
		public string? CustomName { get; private set; } // localized

		public CharacterName(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("name", reader => Name = ParserHelpers.GetString(reader));
			RegisterKeyword("custom_name", reader => CustomName = ParserHelpers.GetString(reader));
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
