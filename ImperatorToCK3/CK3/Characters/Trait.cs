using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Characters;
public sealed class Trait : IIdentifiable<string> {
	public string Id { get; }
	public ISet<string> Opposites { get; private set; } = new HashSet<string>();

	public Trait(string id) {
		Id = id;
	}
	public Trait(string id, BufferedReader traitReader) : this(id) {
		var parser = new Parser();
		parser.RegisterKeyword("opposites", reader => Opposites = reader.GetStrings().ToHashSet());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		parser.ParseStream(traitReader);
	}
}