using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.DeathReason;

public class DeathReasonMapping {
	public SortedSet<string> ImpReasons { get; } = new();
	public string? Ck3Reason { get; set; }

	public DeathReasonMapping(BufferedReader reader) {
		var parser = new Parser();
		parser.RegisterKeyword("ck3", reader => Ck3Reason = reader.GetString());
		parser.RegisterKeyword("imp", reader => ImpReasons.Add(reader.GetString()));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

		parser.ParseStream(reader);
	}
}