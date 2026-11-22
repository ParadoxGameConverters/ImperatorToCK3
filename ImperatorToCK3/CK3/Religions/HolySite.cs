using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.HolySiteEffect;
using System.Collections.Generic;
using System.Text;

namespace ImperatorToCK3.CK3.Religions;

internal sealed class HolySite : IIdentifiable<string>, IPDXSerializable {
	public string Id { get; }
	public bool IsFromConverter { get; }
	public Title? County { get; private set; }
	public Title? Barony { get; }
	public string? CountyId => County?.Id;
	public string? BaronyId => Barony?.Id;
	public bool IsDynamic { get; private set; }
	public OrderedSet<string> Parameters { get; } = [];
	/// <summary>
	/// Modifiers applied to all characters of any faith with this holy site when the holder of the barony is of their faith.
	/// Only applied if the holy site is an eminent holy site for the faith.
	/// </summary>
	public OrderedDictionary<string, object> FaithCharacterModifier { get; } = [];
	public OrderedDictionary<string, object> FaithCharacterModifierScaled { get; } = [];
	/// <summary>
	/// Modifiers applied to the holder of the county of the holy site, if they share its faith.
	/// Applied for both regular and eminent holy sites.
	/// </summary>
	public OrderedDictionary<string, object> CountyHolderCharacterModifier { get; } = [];
	public OrderedDictionary<string, object> CountyModifierScaled { get; } = [];

	public HolySite(string id, BufferedReader holySiteReader, Title.LandedTitles landedTitles, bool isFromConverter) {
		Id = id;
		IsFromConverter = isFromConverter;

		string? parsedCountyId = null;
		string? parsedBaronyId = null;

		var parser = new Parser(implicitVariableHandling: true);
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
		parser.RegisterKeyword("is_dynamic", reader => IsDynamic = reader.GetBool());
		parser.RegisterKeyword("parameters", reader => Parameters.UnionWith(reader.GetStrings()));
		parser.RegisterKeyword("faith_character_modifier", reader => ParseModifierBlock(reader, FaithCharacterModifier));
		parser.RegisterKeyword("faith_character_modifier_scaled", reader => ParseModifierBlock(reader, FaithCharacterModifierScaled));
		parser.RegisterKeyword("county_holder_character_modifier", reader => ParseModifierBlock(reader, CountyHolderCharacterModifier));
		parser.RegisterKeyword("county_modifier_scaled", reader => ParseModifierBlock(reader, CountyModifierScaled));
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

	private static void ParseModifierBlock(BufferedReader reader, OrderedDictionary<string, object> modifierDict) {
		foreach (var assignment in reader.GetAssignments()) {
			modifierDict[assignment.Key] = assignment.Value;
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
			var matches = holySiteEffectMapper.Match(effect, value);
			if (matches is null) {
				continue;
			}

			foreach (var match in matches) {
				ApplyMappedModifier(match);
			}
		}
	}

	private void ApplyMappedModifier(MappedHolySiteModifier match) {
		var ck3Value = (float)match.Value;
		switch (match.BlockKeyword) {
			case MappedHolySiteModifier.FaithCharacterBlock:
				// Faith-wide bonuses only work for eminent holy sites in CK3 1.20+.
				FaithCharacterModifier[match.Modifier] = ck3Value;
				break;
			case MappedHolySiteModifier.FaithCharacterScaledBlock:
				if (match.Scale is not null) {
					FaithCharacterModifierScaled["scale"] = match.Scale;
				}
				FaithCharacterModifierScaled[match.Modifier] = ck3Value;
				break;
			case MappedHolySiteModifier.CountyHolderCharacterBlock:
				// Works for both regular and eminent holy sites.
				CountyHolderCharacterModifier[match.Modifier] = ck3Value;
				break;
			case MappedHolySiteModifier.CountyScaledBlock:
				if (match.Scale is not null) {
					CountyModifierScaled["scale"] = match.Scale;
				}
				CountyModifierScaled[match.Modifier] = ck3Value;
				break;
			default:
				Logger.Warn($"Unknown holy site modifier block {match.BlockKeyword}!");
				break;
		}
	}

	public string Serialize(string indent, bool withBraces) {
		var contentIndent = indent;
		if (withBraces) {
			contentIndent += '\t';
		}

		var sb = new StringBuilder();
		if (withBraces) {
			sb.AppendLine("{");
		}

		if (CountyId is not null) {
			sb.Append(contentIndent).AppendLine($"county = {CountyId}");
		}
		if (BaronyId is not null) {
			sb.Append(contentIndent).AppendLine($"barony = {BaronyId}");
		}
		if (IsDynamic) {
			sb.Append(contentIndent).AppendLine("is_dynamic = yes");
		}
		if (Parameters.Count > 0) {
			sb.Append(contentIndent).AppendLine("parameters = {");
			foreach (var parameter in Parameters) {
				sb.Append(contentIndent).Append('\t').AppendLine(parameter);
			}
			sb.Append(contentIndent).AppendLine("}");
		}

		AppendModifierBlock(sb, contentIndent, "faith_character_modifier", FaithCharacterModifier);
		AppendModifierBlock(sb, contentIndent, "faith_character_modifier_scaled", FaithCharacterModifierScaled);
		AppendModifierBlock(sb, contentIndent, "county_holder_character_modifier", CountyHolderCharacterModifier);
		AppendModifierBlock(sb, contentIndent, "county_modifier_scaled", CountyModifierScaled);

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}

	private static void AppendModifierBlock(
		StringBuilder sb,
		string contentIndent,
		string keyword,
		OrderedDictionary<string, object> modifierDict
	) {
		if (modifierDict.Count == 0) {
			return;
		}

		sb.Append(contentIndent).AppendLine($"{keyword} = {{");
		foreach (var (key, value) in modifierDict) {
			sb.Append(contentIndent).Append('\t').AppendLine($"{key} = {PDXSerializer.Serialize(value)}");
		}
		sb.Append(contentIndent).AppendLine("}");
	}
}