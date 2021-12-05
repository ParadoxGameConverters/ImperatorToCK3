using commonItems;

namespace ImperatorToCK3.Imperator.Characters {
	public class CharacterName : Parser {
		public string Name { get; private set; } = string.Empty; // key for localization or literal name
		public string? CustomName { get; private set; } // localized

		public CharacterName(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("name", reader => Name = reader.GetString());
			RegisterKeyword("custom_name", reader => CustomName = reader.GetString());
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
