using commonItems;
using System;
using System.Linq;

namespace ImperatorToCK3.Imperator.Religions; 

public class ReligionCollection {
	public ReligionCollection() {
		religionParser = new Parser();
		religionParser.RegisterKeyword("modifier", reader => {
			var modifiersAssignments = reader.GetAssignments();
			var modifiers = modifiersAssignments.Select(kvp=>GetModifierValue())
			parsedReligionModifier = 
		});
		
		religionsParser = new Parser();
		religionsParser.RegisterRegex(CommonRegexes.String, (reader, religionId) => {
			parsedReligionId = religionId;
			
		});
	}

	private float GetModifierValue(string valueStr) {
		if (float.TryParse(valueStr, out var parsedValue)) {
			return parsedValue;
		}
		// TODO: READ IMPERATOR game/common/script_values
		if (ScriptValues.TryGetValue(valueStr, out float definedValue)) {
			return definedValue;
		}

		const float defaultValue = 1;
		Logger.Warn($"Could not determine modifier value from string \"{valueStr}\", defaulting to {defaultValue}");
		return defaultValue;
	}

	public void LoadReligions(string filePath) {
		// TODO: use ModFileSystem instead to load from game and mods
	}

	private string parsedReligionId = string.Empty;
	private string parsedReligionModifier = string.Empty;
	private Parser religionsParser;
	private Parser religionParser;
}