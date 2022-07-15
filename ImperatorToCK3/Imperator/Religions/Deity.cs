using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Religions; 

public class Deity : IIdentifiable<string> {
	public string Id { get; }
	public Dictionary<string, float> PassiveModifiers { get; } = new();

	public Deity(string id, BufferedReader deityReader, ScriptValueCollection scriptValues) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("passive_modifier", reader => {
			var modifierValuePairs = reader.GetAssignments()
				.ToDictionary(kvp => kvp.Key, kvp => scriptValues.GetModifierValue(kvp.Value));
			foreach (var (modifierName, value) in modifierValuePairs) {
				PassiveModifiers[modifierName] = value;
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		parser.ParseStream(deityReader);
	}
}