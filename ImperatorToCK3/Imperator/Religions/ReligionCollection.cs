using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Religions; 

public class ReligionCollection : IdObjectCollection<string, Religion> {
	public ReligionCollection(IReadOnlyDictionary<string, float> scriptValues) {
		IDictionary<string, float> parsedReligionModifiers;
		Parser religionParser = new();
		religionParser.RegisterKeyword("modifier", reader => {
			var modifiersAssignments = reader.GetAssignments();
			parsedReligionModifiers = modifiersAssignments
				.ToDictionary(kvp => kvp.Key, kvp => GetModifierValue(kvp.Value, scriptValues));
		});
		religionParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		
		religionsParser = new Parser();
		religionsParser.RegisterRegex(CommonRegexes.String, (reader, religionId) => {
			parsedReligionModifiers = new Dictionary<string, float>();
			
			religionParser.ParseStream(reader);
			AddOrReplace(new Religion(religionId, parsedReligionModifiers));
		});
		religionsParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}

	private static float GetModifierValue(string valueStr, IReadOnlyDictionary<string, float> scriptValues) {
		if (float.TryParse(valueStr, out var parsedValue)) {
			return parsedValue;
		}
		if (scriptValues.TryGetValue(valueStr, out float definedValue)) {
			return definedValue;
		}

		const float defaultValue = 1;
		Logger.Warn($"Could not determine modifier value from string \"{valueStr}\", defaulting to {defaultValue}");
		return defaultValue;
	}

	public void LoadReligions(ModFilesystem imperatorModFS) {
		religionsParser.ParseGameFolder("common/religions", imperatorModFS, "txt", true);
	}

	private readonly Parser religionsParser;
}