using commonItems;

namespace ImperatorToCK3.Imperator.Pops {
	public partial class Pop {
		private static Pop tempPop = new(0);
		private static readonly Parser popParser = new();
		static Pop() {
			popParser.RegisterKeyword("type", reader => tempPop.Type = ParserHelpers.GetString(reader));
			popParser.RegisterKeyword("culture", reader => tempPop.Culture = ParserHelpers.GetString(reader));
			popParser.RegisterKeyword("religion", reader => tempPop.Religion = ParserHelpers.GetString(reader));
			popParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public static Pop Parse(string idString, BufferedReader reader) {
			tempPop = new Pop(ulong.Parse(idString));
			popParser.ParseStream(reader);
			return tempPop;
		}
	}
}
