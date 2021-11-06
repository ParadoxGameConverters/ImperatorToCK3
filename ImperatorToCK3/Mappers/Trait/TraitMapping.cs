using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Trait {
	public class TraitMapping {
		public SortedSet<string> ImpTraits { get; set; } = new();
		public string? CK3Trait { get; set; }

		public TraitMapping(BufferedReader reader) {
			var parser = new Parser();
			parser.RegisterKeyword("ck3", reader => CK3Trait = ParserHelpers.GetString(reader));
			parser.RegisterKeyword("imp", reader => ImpTraits.Add(ParserHelpers.GetString(reader)));
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseStream(reader);
			parser.ClearRegisteredRules();
		}
	}
}
