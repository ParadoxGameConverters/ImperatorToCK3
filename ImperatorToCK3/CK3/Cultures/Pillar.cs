using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Text;

namespace ImperatorToCK3.CK3.Cultures; 

public sealed class Pillar : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }
	public string Type { get; }
	public Color? Color { get; }
	private readonly List<KeyValuePair<string, StringOfItem>> attributes;
	public IReadOnlyCollection<KeyValuePair<string, StringOfItem>> Attributes => attributes;

	public Pillar(string id, PillarData pillarData) {
		Id = id;

		Type = pillarData.Type!;
		Color = pillarData.Color;
		attributes = new List<KeyValuePair<string, StringOfItem>>(pillarData.Attributes);
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

		sb.Append(contentIndent).AppendLine($"type={Type}");
		if (Color is not null) {
			sb.Append(contentIndent).AppendLine($"color={Color}");
		}
		sb.AppendLine(PDXSerializer.Serialize(Attributes, indent: contentIndent, withBraces: false));

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}
}