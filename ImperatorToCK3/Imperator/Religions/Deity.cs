using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Religions;

public sealed class Deity : IIdentifiable<string> {
	public string Id { get; }
	public IDictionary<string, double> PassiveModifiers { get; } = new Dictionary<string, double>();

	public Deity(string id, BufferedReader deityReader, ScriptValueCollection scriptValues) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("passive_modifier", reader => {
			var modifierValuePairs = reader.GetAssignments()
				.ToDictionary(kvp => kvp.Key, kvp => scriptValues.GetValueForString(kvp.Value));
			foreach (var (modifierName, value) in modifierValuePairs) {
				if (value is null) {
					continue;
				}
				PassiveModifiers[modifierName] = (double)value;
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
		parser.ParseStream(deityReader);
	}
}