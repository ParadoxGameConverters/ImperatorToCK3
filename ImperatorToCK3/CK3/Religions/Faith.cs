using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ImperatorToCK3.CK3.Religions; 

public class Faith : IIdentifiable<string>, IPDXSerializable {
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

	public string Serialize(string indent, bool withBraces) {
		var contentIndent = indent;
		if (withBraces) {
			contentIndent += '\t';
		}
		
		var sb = new StringBuilder();
		if (withBraces) {
			sb.AppendLine("{");
		}

		foreach (var holySiteId in HolySiteIds) {
			sb.Append(contentIndent).AppendLine($"holy_site={holySiteId}");
		}

		sb.AppendLine(PDXSerializer.Serialize(attributes, indent: contentIndent, withBraces: false));

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}
}