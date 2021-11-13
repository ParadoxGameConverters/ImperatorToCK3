using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.DeathReason {
	public class DeathReasonMapping : Parser {
		public SortedSet<string> ImpReasons { get; set; } = new();
		public string? Ck3Reason { get; set; }

		public DeathReasonMapping(BufferedReader reader) {
			RegisterKeyword("ck3", reader => Ck3Reason = ParserHelpers.GetString(reader));
			RegisterKeyword("imp", reader => ImpReasons.Add(ParserHelpers.GetString(reader)));
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			ParseStream(reader);
			ClearRegisteredRules();
		}
	}
}
