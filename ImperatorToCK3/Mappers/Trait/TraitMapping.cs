using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Trait;

public sealed class TraitMapping {
	public SortedSet<string> ImperatorTraits { get; } = new();
	public string? CK3Trait { get; set; }

	public TraitMapping(BufferedReader mappingReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ck3", reader => CK3Trait = reader.GetString());
		parser.RegisterKeyword("ir", reader => ImperatorTraits.Add(reader.GetString()));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(mappingReader);
		parser.ClearRegisteredRules();
	}
}