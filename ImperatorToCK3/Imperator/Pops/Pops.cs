using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Pops {
	public class Pops : Dictionary<ulong, Pop> {
		public void LoadPops(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);
			parser.ClearRegisteredRules();
		}
		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, thePopId) => {
				var popStr = new StringOfItem(reader).ToString();
				if (popStr.Contains('{')) {
					var tempStream = new BufferedReader(popStr);
					var pop = Pop.Parse(thePopId, tempStream);
					Add(pop.Id, pop);
				}
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
