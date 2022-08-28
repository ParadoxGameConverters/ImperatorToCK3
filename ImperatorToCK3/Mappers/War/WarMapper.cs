using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.War; 

public class WarMapper {
	private readonly Dictionary<string, string> impToCK3WarGoalDict = new();

	public WarMapper(string filePath) {
		Logger.Info("Parsing wargoal mappings.");

		var parser = new Parser();
		parser.RegisterKeyword("link", (reader) => {
			var mapping = WarMapping.Parse(reader);
			if (mapping.CK3CasusBelli is not null) {
				foreach (var imperatorTrait in mapping.ImperatorWarGoals) {
					impToCK3WarGoalDict.Add(imperatorTrait, mapping.CK3CasusBelli);
				}
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseFile(filePath);

		Logger.Info($"Loaded {impToCK3WarGoalDict.Count} wargoal links.");
	}
	public string? GetCK3CBForImperatorWarGoal(string impWarGoal) {
		if (impToCK3WarGoalDict.TryGetValue(impWarGoal, out var ck3Trait)) {
			return ck3Trait;
		}
		return null;
	}
}