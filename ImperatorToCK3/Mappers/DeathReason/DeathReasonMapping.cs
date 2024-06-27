using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.DeathReason;

public sealed class DeathReasonMapping {
	public SortedSet<string> ImperatorReasons { get; } = new();
	public string? Ck3Reason { get; private set; }

	public DeathReasonMapping(BufferedReader mappingReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ck3", reader => Ck3Reason = reader.GetString());
		parser.RegisterKeyword("ir", reader => ImperatorReasons.Add(reader.GetString()));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

		parser.ParseStream(mappingReader);
	}
}