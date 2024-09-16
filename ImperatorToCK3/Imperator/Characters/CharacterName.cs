using commonItems;

namespace ImperatorToCK3.Imperator.Characters;

public sealed class CharacterName {
	public string Name { get; private set; } = string.Empty; // key for localization or literal name
	public string? CustomName { get; private set; } // localized

	public CharacterName(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("name", reader => Name = reader.GetString());
		parser.RegisterKeyword("custom_name", reader => CustomName = reader.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
}