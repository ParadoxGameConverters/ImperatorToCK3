using commonItems;

namespace ImperatorToCK3.Imperator.Characters {
	public class CharacterName : Parser {
		public string Name { get; private set; } = "";

		public CharacterName(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("name", reader => {
				Name = new SingleString(reader).String;
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
