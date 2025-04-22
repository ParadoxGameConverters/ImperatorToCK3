using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using commonItems.SourceGenerators;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.HolySiteEffect;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Religions;

[SerializationByProperties]
internal sealed partial class HolySite : IIdentifiable<string>, IPDXSerializable {
	[NonSerialized] public string Id { get; }
	[NonSerialized] public bool IsFromConverter { get; }
	[NonSerialized] public Title? County { get; }
	[NonSerialized] public Title? Barony { get; }
	[SerializedName("county")] public string? CountyId => County?.Id;
	[SerializedName("barony")] public string? BaronyId => Barony?.Id;
	[SerializedName("character_modifier")] public OrderedDictionary<string, object> CharacterModifier { get; } = [];
	[SerializedName("flag")] public string? Flag { get; set; }

	public HolySite(string id, BufferedReader holySiteReader, Title.LandedTitles landedTitles, bool isFromConverter) {
		Id = id;
		IsFromConverter = isFromConverter;

		string? parsedCountyId = null;
		string? parsedBaronyId = null;

		var parser = new Parser();
		parser.RegisterKeyword("county_choices", reader => {
			foreach (var countyId in reader.GetStrings()) {
				if (!landedTitles.ContainsKey(countyId)) {
					continue;
				}
				
				parsedCountyId = countyId;
				break;
			}
		});
		parser.RegisterKeyword("county", reader => parsedCountyId = reader.GetString());
		parser.RegisterKeyword("barony", reader => parsedBaronyId = reader.GetString());
		parser.RegisterKeyword("character_modifier", reader => {
			foreach (var assignment in reader.GetAssignments()) {
				CharacterModifier[assignment.Key] = assignment.Value;
			}
		});
		parser.RegisterKeyword("flag", reader => Flag = reader.GetString());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(holySiteReader);

		if (parsedCountyId is not null) {
			County = landedTitles[parsedCountyId];
		}
		if (parsedBaronyId is not null) {
			Barony = landedTitles[parsedBaronyId];
		}

		// Fix "barony not in specified county" errors reported by ck3-tiger.
		if (Barony is not null && County is not null && Barony.DeJureLiege != County) {
			string baseMessage = $"Holy site {Id} has barony {Barony.Id} not in specified county {County.Id}.";
			var correctCounty = Barony.DeJureLiege;
			if (correctCounty is not null) {
				Logger.Debug($"{baseMessage} Setting county to {correctCounty.Id}.");
				County = correctCounty;
			} else {
				Logger.Warn($"{baseMessage} Cannot find correct county.");
			}
		}
	}

	private static string GenerateHolySiteId(Title barony, Faith faith) {
		return $"IRtoCK3_{barony.Id}_{faith.Id}";
	}
	public HolySite(Title barony, Faith faith, Title.LandedTitles titles) {
		IsFromConverter = true;
		Id = GenerateHolySiteId(barony, faith);
		County = titles.GetCountyForProvince(barony.ProvinceId!.Value)!;
		Barony = barony;
	}
	public HolySite(
		Title barony,
		Faith faith,
		Title.LandedTitles titles,
		OrderedDictionary<string, double> imperatorEffects,
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