using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Religions; 

public class Religion : IIdentifiable<string> {
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

	[SerializedName("faiths")] public IdObjectCollection<string, Faith> Faiths { get; } = new();
	[SerializeOnlyValue] private readonly List<KeyValuePair<string, StringOfItem>> attributes = new();
}