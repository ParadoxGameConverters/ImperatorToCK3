using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.HolySiteEffect;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ImperatorToCK3.CK3.Religions; 

public class HolySite : IIdentifiable<string>, IPDXSerializable {
	[NonSerialized] public string Id { get; }
	[NonSerialized] public bool IsGeneratedByConverter { get; }
	[NonSerialized] public Title? County { get; private set; }
	[NonSerialized] public Title? Barony { get; private set; }
	[SerializedName("county")] public string? CountyId => County?.Id;
	[SerializedName("barony")] public string? BaronyId => Barony?.Id;
	[SerializedName("character_modifier")] public Dictionary<string, object> CharacterModifier { get; set; } = new();
	[SerializedName("flag")] public string? Flag { get; set; }
	
	public HolySite(string id, BufferedReader holySiteReader, Title.LandedTitles landedTitles) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("county", reader => County = landedTitles[reader.GetString()]);
		parser.RegisterKeyword("barony", reader => Barony = landedTitles[reader.GetString()]);
		parser.RegisterKeyword("character_modifier", reader => {
			CharacterModifier = reader.GetAssignments()
				.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
		});
		parser.RegisterKeyword("flag", reader => Flag = reader.GetString());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(holySiteReader);
	}

	private static string GenerateHolySiteId(Title barony, Faith faith) {
		return $"IRtoCK3_{barony.Id}_{faith.Id}";
	}
	public HolySite(Title barony, Faith faith, Title.LandedTitles titles) {
		IsGeneratedByConverter = true;
		Id = GenerateHolySiteId(barony, faith);
		County = titles.GetCountyForProvince(barony.Province!.Value)!;
		Barony = barony;
	}
	public HolySite(
		Title barony,
		Faith faith,
		Title.LandedTitles titles,
		IReadOnlyDictionary<string, double> imperatorEffects,
		HolySiteEffectMapper holySiteEffectMapper
	) : this(barony, faith, titles) {
		foreach (var (effect, value) in imperatorEffects) {
			var ck3EffectOpt = holySiteEffectMapper.Match(effect, value);
			if (ck3EffectOpt is not { } ck3Effect) {
				continue;
			}

			CharacterModifier[ck3Effect.Key] = (float)ck3Effect.Value;
		}
	}
}