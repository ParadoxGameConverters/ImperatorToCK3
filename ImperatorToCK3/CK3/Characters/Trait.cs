using commonItems;
using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Characters;
internal sealed class Trait : IIdentifiable<string> {
	public string Id { get; }
	public HashSet<string> Opposites { get; private set; } = [];

	public Trait(string id) {
		Id = id;
	}
	public Trait(string id, BufferedReader traitReader) : this(id) {
		var parser = new Parser();
		parser.RegisterKeyword("opposites", reader => Opposites = [.. reader.GetStrings()]);
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		parser.ParseStream(traitReader);
	}
}