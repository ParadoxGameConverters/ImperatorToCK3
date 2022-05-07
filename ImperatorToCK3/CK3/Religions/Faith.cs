using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Religions; 

public class Faith : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }

	public Faith(string id, BufferedReader faithReader) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("holy_site", reader=>holySites.Add(reader.GetString()));
		parser.RegisterRegex(CommonRegexes.Catchall, (reader, keyword) => {
			attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		parser.ParseStream(faithReader);
	}

	private readonly OrderedSet<string> holySites = new();
	[SerializeOnlyValue] private readonly List<KeyValuePair<string, StringOfItem>> attributes = new();
}