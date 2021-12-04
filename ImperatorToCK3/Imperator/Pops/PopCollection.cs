using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.Imperator.Pops {
	public class PopCollection : IdObjectCollection<ulong, Pop> {
		public void LoadPops(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);
			parser.ClearRegisteredRules();
		}
		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, thePopId) => {
				var popStr = reader.GetStringOfItem().ToString();
				if (popStr.Contains('{')) {
					var tempStream = new BufferedReader(popStr);
					var pop = Pop.Parse(thePopId, tempStream);
					Add(pop);
				}
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
