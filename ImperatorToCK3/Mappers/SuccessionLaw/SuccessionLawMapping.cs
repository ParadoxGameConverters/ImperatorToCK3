using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.SuccessionLaw {
	public class SuccessionLawMapping {
		public string ImperatorLaw { get; set; } = "";
		public SortedSet<string> Ck3SuccessionLaws { get; set; } = new();
		public SuccessionLawMapping(BufferedReader reader) {
			var parser = new Parser();
			parser.RegisterKeyword("imp", (reader) => {
				ImperatorLaw = new SingleString(reader).String;
			});
			parser.RegisterKeyword("ck3", (reader) => {
				Ck3SuccessionLaws.Add(new SingleString(reader).String);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseStream(reader);
			parser.ClearRegisteredRules();
		}
	}
}
