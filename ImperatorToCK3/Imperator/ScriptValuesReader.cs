using commonItems;
using commonItems.Mods;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace ImperatorToCK3.Imperator; 

public static class ScriptValuesReader {
	public static IImmutableDictionary<string, float> GetScriptValues(ModFilesystem modFilesystem) {
		Logger.Info("Reading Imperator script values...");
		var dict = new Dictionary<string, float>();

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, name) => {
			var valueStringOfItem = reader.GetStringOfItem();
			if (valueStringOfItem.IsArrayOrObject()) {
				return;
			}
			Logger.Debug(valueStringOfItem.ToString());

			try {
				dict[name] = float.Parse(valueStringOfItem.ToString(), CultureInfo.InvariantCulture);
			} catch (FormatException e) {
				Logger.Warn($"Can't parse {valueStringOfItem} as float! {e}");
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseGameFolder("common/script_values", modFilesystem, "txt", recursive: true);

		return dict.ToImmutableDictionary();
	}
}