using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.War;

public class WarMapper {
	private readonly Dictionary<string, string> impToCK3WarGoalDict = new();

	public WarMapper(string filePath) {
		Logger.Info("Parsing wargoal mappings...");

		var parser = new Parser();
		parser.RegisterKeyword("link", reader => {
			var mapping = WarMapping.Parse(reader);
			if (mapping.CK3CasusBelli is null) {
				return;
			}

			foreach (var imperatorTrait in mapping.ImperatorWarGoals) {
				impToCK3WarGoalDict.Add(imperatorTrait, mapping.CK3CasusBelli);
			}
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(filePath);

		Logger.Info($"Loaded {impToCK3WarGoalDict.Count} wargoal links.");
		Logger.IncrementProgress();
	}
	public string? GetCK3CBForImperatorWarGoal(string impWarGoal) {
		return impToCK3WarGoalDict.TryGetValue(impWarGoal, out var ck3Trait) ? ck3Trait : null;
	}
}