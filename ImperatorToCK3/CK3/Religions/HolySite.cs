using commonItems;
using commonItems.Collections;
using System;

namespace ImperatorToCK3.CK3.Religions; 

public class HolySite : IIdentifiable<string> {
	public string Id { get; }
	public string? CountyId { get; private set; }
	public string? BaronyId { get; private set; }
	public StringOfItem? CharacterModifier { get; set; }
	public string? Flag { get; set; }

	public HolySite(string id, BufferedReader holySiteReader) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("county", reader => CountyId = reader.GetString());
		parser.RegisterKeyword("barony", reader => BaronyId = reader.GetString());
		parser.RegisterKeyword("character_modifier", reader => CharacterModifier = reader.GetStringOfItem());
		parser.RegisterKeyword("flag", reader => Flag = reader.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(holySiteReader);
	}
}