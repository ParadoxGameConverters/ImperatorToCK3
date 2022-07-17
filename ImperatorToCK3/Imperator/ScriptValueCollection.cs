using commonItems;
using commonItems.Mods;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace ImperatorToCK3.Imperator; 

public class ScriptValueCollection : IReadOnlyDictionary<string, double> {
	private readonly Dictionary<string, double> dict = new();
	public void LoadScriptValues(ModFilesystem modFilesystem) {
		Logger.Info("Reading Imperator script values...");

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, name) => {
			var valueStringOfItem = reader.GetStringOfItem();
			if (valueStringOfItem.IsArrayOrObject()) {
				return;
			}

			try {
				dict[name] = double.Parse(valueStringOfItem.ToString(), CultureInfo.InvariantCulture);
			} catch (FormatException e) {
				Logger.Warn($"Can't parse {valueStringOfItem} as float! {e}");
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseGameFolder("common/script_values", modFilesystem, "txt", recursive: true);
	}

	public IEnumerator<KeyValuePair<string, double>> GetEnumerator() {
		return dict.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	public int Count => dict.Count;
	public bool ContainsKey(string key) => dict.ContainsKey(key);

	public bool TryGetValue(string key, out double value) => dict.TryGetValue(key, out value);

	public double this[string key] => dict[key];

	public IEnumerable<string> Keys => dict.Keys;
	public IEnumerable<double> Values => dict.Values;
	
	public double GetModifierValue(string valueStr) {
		if (double.TryParse(valueStr, CultureInfo.InvariantCulture, out var parsedValue)) {
			return parsedValue;
		}
		if (TryGetValue(valueStr, out double definedValue)) {
			return definedValue;
		}

		const double defaultValue = 1;
		Logger.Warn($"Could not determine modifier value from string \"{valueStr}\", defaulting to {defaultValue}");
		return defaultValue;
	}
}