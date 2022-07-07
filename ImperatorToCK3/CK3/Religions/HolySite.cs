using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.HolySiteEffect;
using System;

namespace ImperatorToCK3.CK3.Religions; 

public class HolySite : IIdentifiable<string>, IPDXSerializable {
	[commonItems.Serialization.NonSerialized] public string Id { get; }
	[commonItems.Serialization.NonSerialized] public bool IsGeneratedByConverter { get; }
	[SerializedName("county")] public string? CountyId { get; private set; }
	[SerializedName("barony")] public string? BaronyId { get; private set; }
	[SerializedName("character_modifier")] public StringOfItem? CharacterModifier { get; set; }
	[SerializedName("flag")] public string? Flag { get; set; }
	
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
		IsGeneratedByConverter = true;
		Id = $"IRtoCK3_{barony.Id}_{faith.Id}";
		CountyId = titles.GetCountyForProvince((ulong)barony.Province!)!.Id;
		BaronyId = barony.Id;
	}
	public HolySite(Title barony, Faith faith, Title.LandedTitles titles, HolySiteEffectMapper holySiteEffectMapper): this(barony, faith, titles) {
		// TODO: CONVERT MODIFIER FROM EITHER IMPERATOR RELIGION OR DEITY
		throw new NotImplementedException();
	}
}