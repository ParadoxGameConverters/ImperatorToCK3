using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Nickname;

public sealed class NicknameMapping {
	public SortedSet<string> ImperatorNicknames { get; } = new();
	public string? CK3Nickname { get; private set; }

	public NicknameMapping(BufferedReader mappingReader) {
		var parser = new Parser();
		parser.RegisterKeyword("ck3", reader => CK3Nickname = reader.GetString());
		parser.RegisterKeyword("ir", reader => ImperatorNicknames.Add(reader.GetString()));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(mappingReader);
	}
}