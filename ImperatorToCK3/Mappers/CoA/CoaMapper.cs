using commonItems;
using commonItems.Mods;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.CoA;

public sealed class CoaMapper {
	public CoaMapper() { }
	public CoaMapper(ModFilesystem imperatorModFS) {
		Logger.Info("Parsing CoAs...");
		var parser = new Parser();
		RegisterKeys(parser);
		const string coasPath = "common/coat_of_arms/coat_of_arms";
		parser.ParseGameFolder(coasPath, imperatorModFS, "txt", true);

		Logger.Info($"Loaded {coasMap.Count} coats of arms.");

		Logger.IncrementProgress();
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("template", ParserHelpers.IgnoreItem); // we don't need templates, we need CoAs!
		parser.RegisterRegex(CommonRegexes.Catchall, (reader, flagName) => coasMap[flagName] = reader.GetStringOfItem().ToString());
	}

	public void ParseCoAs(IEnumerable<string> coaDefinitionStrings) {
		var parser = new Parser();
		RegisterKeys(parser);
		foreach (var coaDefinitionString in coaDefinitionStrings) {
			parser.ParseStream(new BufferedReader(coaDefinitionString));
		}
	}

	public string? GetCoaForFlagName(string irFlagName) {
		if (!coasMap.TryGetValue(irFlagName, out string? value)) {
			Logger.Warn($"No CoA defined for flag name {irFlagName}.");
			return null;
		}

		return value;
	}
	
	/// For a given collection of flag names, returns ones that don't have a defined CoA.
	public ISet<string> GetAllMissingFlagKeys(IEnumerable<string> flagKeys) {
		var existingFlagKeys = coasMap.Keys.ToHashSet();
		return flagKeys.Where(flagKey => !existingFlagKeys.Contains(flagKey)).ToHashSet();
	}

	private readonly Dictionary<string, string> coasMap = new();
}