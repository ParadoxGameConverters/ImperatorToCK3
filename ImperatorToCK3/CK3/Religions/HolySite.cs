using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.HolySiteEffect;
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

	public HolySite(Title barony, Faith faith, Title.LandedTitles titles) {
		Id = $"IRtoCK3_site_{barony.Id}_{faith.Id}";
		CountyId = titles.GetCountyForProvince((ulong)barony.Province!)!.Id;
		BaronyId = barony.Id;
	}
	public HolySite(Title barony, Faith faith, Title.LandedTitles titles, HolySiteEffectMapper holySiteEffectMapper): this(barony, faith, titles) {
		// TODO: CONVERT MODIFIER FROM EITHER IMPERATOR RELIGION OR DEITY
		throw new NotImplementedException();
	}
}