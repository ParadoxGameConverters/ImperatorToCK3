using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.HolySiteEffect;

/// <summary>
/// A CK3 modifier produced by a holy site effect mapping.
/// </summary>
/// <param name="BlockKeyword">Modifier block of the holy site the modifier belongs to.</param>
/// <param name="Modifier">CK3 modifier key.</param>
/// <param name="Value">Modifier value: Imperator value multiplied by the mapping's factor.</param>
/// <param name="Scale">Optional scale value of the block, e.g. holy_site_total_artifact_rarity.</param>
internal sealed record MappedHolySiteModifier(string BlockKeyword, string Modifier, double Value, string? Scale = null) {
	public const string FaithCharacterBlock = "faith_character_modifier";
	public const string FaithCharacterScaledBlock = "faith_character_modifier_scaled";
	public const string CountyHolderCharacterBlock = "county_holder_character_modifier";
	public const string CountyScaledBlock = "county_modifier_scaled";

	public static string[] BlockKeywords { get; } = [
		FaithCharacterBlock,
		FaithCharacterScaledBlock,
		CountyHolderCharacterBlock,
		CountyScaledBlock
	];
}

/// <summary>
/// Maps Imperator religion modifiers and deity passive effects to CK3 1.20+ holy site
/// modifier blocks. Each link maps an Imperator effect to modifiers in the holy site's
/// modifier blocks, and every block can contain multiple ck3-factor pairs:
/// link = {
///     ir = X
///     faith_character_modifier = { ck3 = Y factor = N ... }
///     faith_character_modifier_scaled = { scale = S ck3 = Y factor = N ... }
///     county_holder_character_modifier = { ck3 = Y factor = N ... }
///     county_modifier_scaled = { scale = S ck3 = Y factor = N ... }
/// }
/// </summary>
internal sealed class HolySiteEffectMapper {
	private sealed record MappingTarget(string BlockKeyword, string Ck3Modifier, double Factor, string? Scale);

	private readonly Dictionary<string, IReadOnlyCollection<MappingTarget>> effectMap = new(); // imperator effect, targets

	public HolySiteEffectMapper(string mappingsFilePath) {
		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterKeyword("link", ParseLink);
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseFile(mappingsFilePath);
	}

	private void ParseLink(BufferedReader mappingReader) {
		string? ir = null;
		var targets = new List<MappingTarget>();

		var mappingParser = new Parser(implicitVariableHandling: true);
		mappingParser.RegisterKeyword("ir", reader => ir = reader.GetString());
		foreach (var blockKeyword in MappedHolySiteModifier.BlockKeywords) {
			mappingParser.RegisterKeyword(blockKeyword, blockReader => ParseBlock(blockReader, blockKeyword, targets));
		}
		mappingParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		mappingParser.ParseStream(mappingReader);

		if (ir is null) {
			Logger.Warn("Holy site effect mapping has no ir entry!");
			return;
		}

		if (targets.Count == 0) {
			Logger.Warn($"Holy site effect mapping for {ir} has no modifier blocks!");
			return;
		}

		effectMap[ir] = targets;
	}

	private static void ParseBlock(BufferedReader blockReader, string blockKeyword, List<MappingTarget> targets) {
		string? scale = null;
		string? currentModifier = null;

		var blockParser = new Parser(implicitVariableHandling: true);
		blockParser.RegisterKeyword("scale", reader => scale = reader.GetString());
		blockParser.RegisterKeyword("ck3", reader => currentModifier = reader.GetString());
		blockParser.RegisterKeyword("factor", reader => {
			var factor = reader.GetDouble();
			if (currentModifier is null) {
				Logger.Warn($"Holy site effect mapping block {blockKeyword} has a factor with no preceding ck3 modifier!");
				return;
			}

			targets.Add(new MappingTarget(blockKeyword, currentModifier, factor, scale));
			currentModifier = null;
		});
		blockParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		blockParser.ParseStream(blockReader);
	}

	public IReadOnlyCollection<MappedHolySiteModifier>? Match(string imperatorEffect, double imperatorValue) {
		if (!effectMap.TryGetValue(imperatorEffect, out var targets)) {
			return null;
		}

		return [.. targets.Select(target => new MappedHolySiteModifier(
			BlockKeyword: target.BlockKeyword,
			Modifier: target.Ck3Modifier,
			Value: imperatorValue * target.Factor,
			Scale: target.Scale
		))];
	}
}
