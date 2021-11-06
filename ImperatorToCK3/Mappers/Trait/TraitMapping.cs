using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Trait {
	public class TraitMapping {
		public SortedSet<string> ImpTraits { get; set; } = new();
		public string? Ck3Trait { get; set; }

		public TraitMapping(BufferedReader reader) {
			var parser = new Parser();
			parser.RegisterKeyword("ck3", (reader) => {
				Ck3Trait = new SingleString(reader).String;
			});
			parser.RegisterKeyword("imp", (reader) => {
				ImpTraits.Add(new SingleString(reader).String);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseStream(reader);
			parser.ClearRegisteredRules();
		}
	}
}
