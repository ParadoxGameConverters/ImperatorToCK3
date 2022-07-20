using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace ImperatorToCK3.CK3.Religions; 

public class Faith : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }
	public Color? Color { get; private set; }
	public bool ModifiedByConverter { get; private set; } = false;

	public Faith(string id, BufferedReader faithReader) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("holy_site", reader => holySiteIds.Add(reader.GetString()));
		parser.RegisterKeyword("color", reader => Color = colorFactory.GetColor(reader));
		parser.RegisterRegex(CommonRegexes.String, (reader, keyword) => {
			attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(faithReader);
	}

	private readonly OrderedSet<string> holySiteIds = new();
	public IReadOnlyCollection<string> HolySiteIds => holySiteIds.ToImmutableArray();
	private readonly List<KeyValuePair<string, StringOfItem>> attributes = new();

	public void ReplaceHolySiteId(string oldId, string newId) {
		if (holySiteIds.Remove(oldId)) {
			holySiteIds.Add(newId);
			ModifiedByConverter = true;
		} else {
			Logger.Warn($"{oldId} does not belong to holy sites of faith {Id} and cannot be replaced!");
		}
	}

	public string Serialize(string indent, bool withBraces) {
		var contentIndent = indent;
		if (withBraces) {
			contentIndent += '\t';
		}
		
		var sb = new StringBuilder();
		if (withBraces) {
			sb.AppendLine("{");
		}

		if (Color is not null) {
			sb.Append(contentIndent).AppendLine($"color={Color.OutputRgb()}");
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

	private static readonly ColorFactory colorFactory = new();
}