using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.HolySiteEffect;
using System.Collections.Generic;
using System.Globalization;

namespace ImperatorToCK3.CK3.Religions; 

public class HolySite : IIdentifiable<string>, IPDXSerializable {
	[NonSerialized] public string Id { get; }
	[NonSerialized] public bool IsGeneratedByConverter { get; }
	[SerializedName("county")] public string? CountyId { get; private set; }
	[SerializedName("barony")] public string? BaronyId { get; private set; }
	[SerializedName("character_modifier")] public Dictionary<string, string> CharacterModifier { get; set; } = new();
	[SerializedName("flag")] public string? Flag { get; set; }
	
	public HolySite(string id, BufferedReader holySiteReader) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("county", reader => CountyId = reader.GetString());
		parser.RegisterKeyword("barony", reader => BaronyId = reader.GetString());
		parser.RegisterKeyword("character_modifier", reader => CharacterModifier = reader.GetAssignments());
		parser.RegisterKeyword("flag", reader => Flag = reader.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(holySiteReader);
	}

	private static string GenerateHolySiteId(Title barony, Faith faith) {
		return $"IRtoCK3_{barony.Id}_{faith.Id}";
	}
	public HolySite(Title barony, Faith faith, Title.LandedTitles titles) {
		IsGeneratedByConverter = true;
		Id = GenerateHolySiteId(barony, faith);
		CountyId = titles.GetCountyForProvince((ulong)barony.Province!)!.Id;
		BaronyId = barony.Id;
	}
	public HolySite(
		Title barony,
		Faith faith,
		Title.LandedTitles titles,
		IReadOnlyDictionary<string, float> imperatorEffects,
		HolySiteEffectMapper holySiteEffectMapper
	) : this(barony, faith, titles) {
		foreach (var (effect, value) in imperatorEffects) {
			var ck3EffectOpt = holySiteEffectMapper.Match(effect, value);
			if (ck3EffectOpt is not { } ck3Effect) {
				continue;
			}

			CharacterModifier[ck3Effect.Key] = ck3Effect.Value.ToString(CultureInfo.InvariantCulture);
		}
	}
}