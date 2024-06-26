using System.Collections.Generic;
using commonItems;
using commonItems.Mods;

namespace ImperatorToCK3.Mappers.War;

public sealed class WarMapper {
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
	
	public string? GetCK3CBForImperatorWarGoal(string irWarGoal) {
		if (impToCK3WarGoalDict.TryGetValue(irWarGoal, out var ck3CasusBelli)) {
			return ck3CasusBelli;
		}
		Logger.Warn($"No CK3 casus belli found for Imperator war goal {irWarGoal}");
		return null;
	}

	public void DetectUnmappedWarGoals(ModFilesystem irModFS) {
		Logger.Info("Detecting unmapped war goals...");
		
		var warGoalsParser = new Parser();
		warGoalsParser.RegisterRegex(CommonRegexes.String, (reader, warGoal) => {
			if (!impToCK3WarGoalDict.ContainsKey(warGoal)) {
				Logger.Warn($"No mapping for war goal {warGoal} found in war goal mappings!");
			}
			ParserHelpers.IgnoreItem(reader);
		});
		warGoalsParser.IgnoreAndLogUnregisteredItems();
		warGoalsParser.ParseGameFolder("common/wargoals", irModFS, "txt", recursive: true, parallel: true);
	}
}