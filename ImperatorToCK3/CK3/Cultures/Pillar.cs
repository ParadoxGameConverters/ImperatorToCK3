using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Text;

namespace ImperatorToCK3.CK3.Cultures; 

public class Pillar : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }
	public string Type { get; }
	private readonly List<KeyValuePair<string, StringOfItem>> attributes;
	public IReadOnlyCollection<KeyValuePair<string, StringOfItem>> Attributes => attributes;

	public Pillar(string id, PillarData pillarData) {
		Id = id;

		Type = pillarData.Type!;
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
		sb.AppendLine(PDXSerializer.Serialize(Attributes, indent: contentIndent, withBraces: false));

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}
}