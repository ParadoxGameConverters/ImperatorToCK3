using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Text;

namespace ImperatorToCK3.CK3.Religions;

public class Religion : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }

	public Religion(string id, BufferedReader religionReader, ReligionCollection religions, ColorFactory colorFactory) {
		Id = id;

		var religionParser = new Parser();
		religionParser.RegisterKeyword("faiths", faithsReader => {
			var faithsParser = new Parser();
			faithsParser.RegisterRegex(CommonRegexes.String, (faithReader, faithId) => {
				// The faith might have already been added to another religion.
				foreach (var otherReligion in religions) {
					otherReligion.Faiths.Remove(faithId);
				}

				Faiths.AddOrReplace(new Faith(faithId, faithReader, colorFactory));
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