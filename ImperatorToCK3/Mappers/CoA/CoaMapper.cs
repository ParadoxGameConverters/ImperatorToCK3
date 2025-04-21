using commonItems;
using commonItems.Mods;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.CoA;

internal sealed class CoaMapper {
	public CoaMapper() { }
	public CoaMapper(ModFilesystem modFS) {
		Logger.Info("Parsing CoAs...");
		var parser = new Parser();
		RegisterKeys(parser);
		const string coasPath = "common/coat_of_arms/coat_of_arms";
		parser.ParseGameFolder(coasPath, modFS, "txt", recursive: true);

		Logger.Info($"Loaded {coasMap.Count} coats of arms.");

		Logger.IncrementProgress();
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterRegex(CommonRegexes.Variable, (reader, variableName) => { // for variables like "@smCross = 0.22"
			var variableValue = reader.GetString();
			variablesToOutput[variableName[1..]] = variableValue;
		});
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

	public string? GetCoaForFlagName(string flagName, bool warnIfMissing) {
		if (!coasMap.TryGetValue(flagName, out string? value)) {
			if (warnIfMissing) {
				Logger.Warn($"No CoA defined for flag name {flagName}.");
			}
			return null;
		}

		return value;
	}
	
	/// <summary>
	/// For a given collection of flag names, returns ones that don't have a defined CoA.
	/// </summary>
	public HashSet<string> GetAllMissingFlagKeys(IEnumerable<string> flagKeys) {
		var existingFlagKeys = coasMap.Keys.ToHashSet();
		return flagKeys.Where(flagKey => !existingFlagKeys.Contains(flagKey)).ToHashSet();
	}

	private readonly Dictionary<string, string> coasMap = [];
	
	private readonly Dictionary<string, object> variablesToOutput = new();
	public IReadOnlyDictionary<string, object> VariablesToOutput => variablesToOutput;
}