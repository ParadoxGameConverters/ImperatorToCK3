using commonItems;

namespace ImperatorToCK3.Imperator.Provinces;

public sealed class ProvinceName {
	public string Name { get; private set; } = "";

	public ProvinceName(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("name", reader => Name = reader.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
}