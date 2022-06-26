using commonItems;
using commonItems.Mods;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ImperatorToCK3.Imperator; 

public static class ScriptValuesReader {
	public static IImmutableDictionary<string, float> GetScriptValues(ModFilesystem modFilesystem) {
		var dict = new Dictionary<string, float>();
		
		var files = modFilesystem.GetAllFilesInFolderRecursive("/common/script_values");
		if (files.Count == 0) {
			return dict.ToImmutableDictionary();
		}

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, name) => {
			var valueStr = reader.GetStringOfItem();
			if (valueStr.IsArrayOrObject()) {
				return;
			}

			dict[name] = float.Parse(valueStr.ToString());
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		
		foreach (var file in files) {
			parser.ParseFile(file);
		}

		return dict.ToImmutableDictionary();
	}
}