using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CK3.Religions;

internal sealed class Religion : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }
	public OrderedSet<string> DoctrineIds { get; } = [];

	public ReligionCollection ReligionCollection { get; }
	private readonly OrderedDictionary<string, StringOfItem> localization = [];
	private readonly OrderedDictionary<string, StringOfItem> detailsAttributes = [];

	public Religion(string id, BufferedReader religionReader, ReligionCollection religions) {
		Id = id;
		ReligionCollection = religions;

		var religionParser = new Parser(implicitVariableHandling: true);
		religionParser.RegisterKeyword("doctrine", reader => DoctrineIds.Add(reader.GetString()));
		// CK3 1.20+ religions define their family and other details in a religion_details block.
		religionParser.RegisterKeyword("religion_details", reader => {
			foreach (var assignment in reader.GetAssignments()) {
				detailsAttributes[assignment.Key] = new StringOfItem(assignment.Value);
			}
		});
		religionParser.RegisterKeyword("localization", reader => {
			var localizationParser = new Parser(implicitVariableHandling: true);
			localizationParser.RegisterRegex(CommonRegexes.Catchall, (locReader, locKey) => {
				localization[locKey] = locReader.GetStringOfItem();
			});
			localizationParser.ParseStream(reader);
		});
		religionParser.RegisterRegex(CommonRegexes.Catchall, (reader, keyword) => {
			attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		religionParser.ParseStream(religionReader);

		// Fix a religion having more than one doctrine in the same group.
		foreach (var group in religions.DoctrineGroups) {
			var doctrinesInGroup = DoctrineIds.Intersect(group.DoctrineIds).ToList();
			if (doctrinesInGroup.Count <= 1) {
				continue;
			}

			Logger.Warn($"Religion {Id} has too many doctrines in group {group.Id}: " +
			            $"{string.Join(", ", doctrinesInGroup)}. Keeping the last of them.");

			DoctrineIds.ExceptWith(doctrinesInGroup);
			DoctrineIds.Add(doctrinesInGroup[^1]);
		}
	}

	/// <summary>
	/// ID of the religion family, e.g. rf_abrahamic. Defined in the religion_details block in CK3 1.20+.
	/// </summary>
	public string? FamilyId => detailsAttributes.TryGetValue("family", out var family) ? family.ToString().RemQuotes() : null;

	public IdObjectCollection<string, Faith> Faiths { get; } = [];
	private readonly List<KeyValuePair<string, StringOfItem>> attributes = [];

	public string Serialize(string indent, bool withBraces) {
		var contentIndent = indent;
		if (withBraces) {
			contentIndent += '\t';
		}

		var sb = new StringBuilder();
		if (withBraces) {
			sb.AppendLine("{");
		}

		sb.Append(contentIndent).AppendLine("religion_details = {");
		var detailsIndent = contentIndent + '\t';
		foreach (var (key, value) in detailsAttributes) {
			sb.Append(detailsIndent).AppendLine($"{key} = {value}");
		}
		sb.Append(contentIndent).AppendLine("}");

		foreach (var doctrineId in DoctrineIds) {
			sb.Append(contentIndent).AppendLine($"doctrine={doctrineId}");
		}

		sb.Append(contentIndent).AppendLine("localization = {");
		foreach (var (key, value) in localization) {
			sb.Append(contentIndent).Append('\t').AppendLine($"{key}={value}");
		}
		sb.Append(contentIndent).AppendLine("}");

		sb.AppendLine(PDXSerializer.Serialize(attributes, indent: contentIndent+'\t', withBraces: false));

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}
}