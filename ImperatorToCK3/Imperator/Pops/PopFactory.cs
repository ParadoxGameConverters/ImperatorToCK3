using commonItems;

namespace ImperatorToCK3.Imperator.Pops {
	public partial class Pop {
		private static Pop tempPop = new(0);
		private static readonly Parser popParser = new();
		static Pop() {
			popParser.RegisterKeyword("type", (sr) => {
				tempPop.Type = new SingleString(sr).String;
			});
			popParser.RegisterKeyword("culture", (sr) => {
				tempPop.Culture = new SingleString(sr).String;
			});
			popParser.RegisterKeyword("religion", (sr) => {
				tempPop.Religion = new SingleString(sr).String;
			});
			popParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public static Pop Parse(string idString, BufferedReader reader) {
			tempPop = new Pop(ulong.Parse(idString));
			popParser.ParseStream(reader);
			return tempPop;
		}
	}
}
