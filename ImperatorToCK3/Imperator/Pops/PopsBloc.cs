using commonItems;

namespace ImperatorToCK3.Imperator.Pops;

internal class PopsBloc {
	public PopCollection PopsFromBloc { get; private set; } = new();
	public PopsBloc(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("population", reader => PopsFromBloc.LoadPops(reader));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
}