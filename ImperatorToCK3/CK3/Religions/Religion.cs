using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Text;

namespace ImperatorToCK3.CK3.Religions; 

public class Religion : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }

	public Religion(string id, BufferedReader religionReader) {
		Id = id;

		var religionParser = new Parser();
		religionParser.RegisterKeyword("faiths", faithsReader => {
			var faithsParser = new Parser();
			faithsParser.RegisterRegex(CommonRegexes.String, (faithReader, faithId) => {
				Faiths.Add(new Faith(faithId, faithReader));
			});
			faithsParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
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