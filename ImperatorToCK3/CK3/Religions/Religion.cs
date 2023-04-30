using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Text;

namespace ImperatorToCK3.CK3.Religions;

public class Religion : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }
	public OrderedSet<string> DoctrineIds { get; } = new();

	public ReligionCollection ReligionCollection { get; }

	public Religion(string id, BufferedReader religionReader, ReligionCollection religions, ColorFactory colorFactory) {
		Id = id;
		ReligionCollection = religions;

		var religionParser = new Parser();
		religionParser.RegisterKeyword("doctrine", reader => DoctrineIds.Add(reader.GetString()));
		religionParser.RegisterKeyword("faiths", faithsReader => {
			var faithsParser = new Parser();
			faithsParser.RegisterRegex(CommonRegexes.String, (faithReader, faithId) => {
				// The faith might have already been added to another religion.
				foreach (var otherReligion in ReligionCollection) {
					otherReligion.Faiths.Remove(faithId);
				}

				Faiths.AddOrReplace(new Faith(faithId, faithReader, this, colorFactory));
			});
			faithsParser.IgnoreAndLogUnregisteredItems();
			faithsParser.ParseStream(faithsReader);
		});
		religionParser.RegisterRegex(CommonRegexes.Catchall, (reader, keyword) => {
			attributes.Add(new KeyValuePair<string, StringOfItem>(keyword, reader.GetStringOfItem()));
		});
		religionParser.ParseStream(religionReader);
	}

	public IdObjectCollection<string, Faith> Faiths { get; } = new();
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
		
		foreach (var doctrineId in DoctrineIds) {
			sb.Append(contentIndent).AppendLine($"doctrine={doctrineId}");
		}
		sb.AppendLine(PDXSerializer.Serialize(attributes, indent: contentIndent+'\t', withBraces: false));

		sb.Append(contentIndent).AppendLine("faiths={");
		sb.AppendLine(PDXSerializer.Serialize(Faiths, contentIndent+'\t'));
		sb.Append(contentIndent).AppendLine("}");

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}
}