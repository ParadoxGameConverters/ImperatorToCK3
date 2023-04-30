using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CK3.Religions;

public class Faith : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }
	public Religion Religion { get; }
	public Color? Color { get; private set; }
	public string? ReligiousHeadTitleId { get; private set; }
	public bool ModifiedByConverter { get; private set; } = false;
	public OrderedSet<string> DoctrineIds { get; } = new();

	public Faith(string id, BufferedReader faithReader, Religion religion, ColorFactory colorFactory) {
		Id = id;
		Religion = religion;

		var parser = new Parser();
		parser.RegisterKeyword("color", reader => Color = colorFactory.GetColor(reader));
		parser.RegisterKeyword("religious_head", reader => ReligiousHeadTitleId = reader.GetString());
		parser.RegisterKeyword("holy_site", reader => holySiteIds.Add(reader.GetString()));
		parser.RegisterKeyword("doctrine", reader => DoctrineIds.Add(reader.GetString()));
		parser.RegisterRegex(CommonRegexes.String, (reader, keyword) => {
			attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		parser.IgnoreAndLogUnregisteredItems();
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
		if (ReligiousHeadTitleId is not null) {
			sb.Append(contentIndent).AppendLine($"religious_head={ReligiousHeadTitleId}");
		}
		foreach (var holySiteId in HolySiteIds) {
			sb.Append(contentIndent).AppendLine($"holy_site={holySiteId}");
		}
		foreach (var doctrineId in DoctrineIds) {
			sb.Append(contentIndent).AppendLine($"doctrine={doctrineId}");
		}
		
		sb.AppendLine(PDXSerializer.Serialize(attributes, indent: contentIndent, withBraces: false));

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}

	public string? GetDoctrineIdForDoctrineCategoryId(string doctrineCategoryId) {
		var category = Religion.ReligionCollection.DoctrineCategories[doctrineCategoryId];
		var potentialDoctrineIds = category.DoctrineIds;
		
		// Look in faith first. If not found, look in religion.
		var matchingInFaith = DoctrineIds.Intersect(potentialDoctrineIds).LastOrDefault();
		return matchingInFaith ?? Religion.DoctrineIds.Intersect(potentialDoctrineIds).LastOrDefault();
	}
}