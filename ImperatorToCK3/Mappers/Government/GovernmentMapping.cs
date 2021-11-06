using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Government {
	public class GovernmentMapping {
		public SortedSet<string> ImperatorGovernments { get; } = new();
		public string Ck3Government { get; private set; } = "";
		public GovernmentMapping(BufferedReader reader) {
			var parser = new Parser();
			parser.RegisterKeyword("ck3", reader => Ck3Government = ParserHelpers.GetString(reader));
			parser.RegisterKeyword("imp", reader => ImperatorGovernments.Add(ParserHelpers.GetString(reader)));
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			parser.ParseStream(reader);
			parser.ClearRegisteredRules();
		}
	}
}
