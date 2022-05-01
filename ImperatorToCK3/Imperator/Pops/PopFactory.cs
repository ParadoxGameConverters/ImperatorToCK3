using commonItems;

namespace ImperatorToCK3.Imperator.Pops;

public partial class Pop {
	static Pop() {
		popParser.RegisterKeyword("type", reader => tempPop.Type = reader.GetString());
		popParser.RegisterKeyword("culture", reader => tempPop.Culture = reader.GetString());
		popParser.RegisterKeyword("religion", reader => tempPop.Religion = reader.GetString());
		popParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public static Pop Parse(string idString, BufferedReader reader) {
		tempPop = new Pop(ulong.Parse(idString));
		popParser.ParseStream(reader);
		return tempPop;
	}

	private static Pop tempPop = new(0);
	private static readonly Parser popParser = new();
}