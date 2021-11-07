using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.SuccessionLaw {
	public class SuccessionLawMapping {
		public string ImperatorLaw { get; set; } = "";
		public SortedSet<string> CK3SuccessionLaws { get; set; } = new();
		public SuccessionLawMapping(BufferedReader reader) {
			var parser = new Parser();
			parser.RegisterKeyword("imp", reader =>
				ImperatorLaw = ParserHelpers.GetString(reader)
			);
			parser.RegisterKeyword("ck3", reader =>
				CK3SuccessionLaws.Add(ParserHelpers.GetString(reader))
			);
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseStream(reader);
			parser.ClearRegisteredRules();
		}
	}
}
