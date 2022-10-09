using commonItems;
using commonItems.Collections;
using System;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Cultures; 

public class Culture : IIdentifiable<string> {
	public string Id { get; }
	public Color Color { get; private set; }
	public string NameListId { get; private set; }
	public string HeritageId { get; private set; }
	public string LanguageId { get; private set; }
	public string EthosId { get; private set; }
	public string MartialCustomId { get; private set; }
	public OrderedSet<string> Traditions { get; } = new();

	public Culture(string id, BufferedReader cultureReader) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("color", reader => Color = ColorFactory.GetColor(reader));
		parser.RegisterKeyword("name_list", reader => NameListId = reader.GetString());
		parser.RegisterKeyword("heritage", reader => HeritageId = reader.GetString());
		parser.RegisterKeyword("language", reader => LanguageId = reader.GetString());
		parser.RegisterKeyword("ethos", reader => EthosId = reader.GetString());
		parser.RegisterKeyword("martial_custom", reader => MartialCustomId = reader.GetString());
		parser.RegisterKeyword("traditions", reader => Traditions.UnionWith(reader.GetStrings()));
		parser.IgnoreAndStoreUnregisteredItems(IgnoredKeywords);
		parser.ParseStream(cultureReader);
	}

	public static ColorFactory ColorFactory { get; } = new();
	public static HashSet<string> IgnoredKeywords { get; } = new();
}