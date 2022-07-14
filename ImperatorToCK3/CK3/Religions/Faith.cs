using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Religions; 

public class Faith : IIdentifiable<string> {
	public string Id { get; }
	public bool ModifiedByConverter { get; set; } = false;

	public Faith(string id, BufferedReader faithReader) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("holy_site", reader => HolySiteIds.Add(reader.GetString()));
		parser.RegisterRegex(CommonRegexes.Catchall, (reader, keyword) => {
			attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		parser.ParseStream(faithReader);
	}

	public OrderedSet<string> HolySiteIds { get; } = new();
	private readonly List<KeyValuePair<string, StringOfItem>> attributes = new();
}