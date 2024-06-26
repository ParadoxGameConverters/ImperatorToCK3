using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.SuccessionLaw;

public sealed class SuccessionLawMapping {
	public string ImperatorLaw { get; set; } = "";
	public SortedSet<string> CK3SuccessionLaws { get; } = new();
	public SuccessionLawMapping(BufferedReader mappingReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ir", reader => ImperatorLaw = reader.GetString());
		parser.RegisterKeyword("ck3", reader => CK3SuccessionLaws.Add(reader.GetString()));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(mappingReader);
	}
}