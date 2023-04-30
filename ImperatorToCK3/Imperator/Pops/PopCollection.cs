using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.Imperator.Pops;

public sealed class PopCollection : IdObjectCollection<ulong, Pop> {
	public void LoadPopsFromBloc(BufferedReader blocReader) {
		var blocParser = new Parser();
		blocParser.RegisterKeyword("population", LoadPops);
		blocParser.IgnoreAndLogUnregisteredItems();
		blocParser.ParseStream(blocReader);
	}
	public void LoadPops(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterRegex(CommonRegexes.Integer, (reader, thePopId) => {
			var popStr = reader.GetStringOfItem().ToString();
			if (!popStr.Contains('{')) {
				return;
			}
			var tempStream = new BufferedReader(popStr);
			var pop = Pop.Parse(thePopId, tempStream);
			Add(pop);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
}