using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Diplomacy; 

public class War {
	public Date StartDate { get; private set; } = new(1, 1, 1);
	public bool Previous { get; private set; }
	public List<ulong> AttackerCountryIds { get; } = new();
	public List<ulong> DefenderCountryIds { get; } = new();
	public string? WarGoal { get; private set; }
	public string? TargetedState { get; private set; } // TODO: use this when importing to CK3

	static War() {
		parser.RegisterKeyword("start_date", reader => {
			warToReturn.StartDate = new Date(reader.GetString(), AUC: true);
		});
		parser.RegisterKeyword("attacker", reader => {
			warToReturn.AttackerCountryIds.Add(reader.GetULong());
		});
		parser.RegisterKeyword("defender", reader => {
			warToReturn.DefenderCountryIds.Add(reader.GetULong());
		});
		parser.RegisterRegex(WargoalTypeRegex, reader => {
			var wargoalParser = new Parser();
			wargoalParser.RegisterKeyword("type", typeReader =>
				warToReturn.WarGoal = typeReader.GetString()
			);
			wargoalParser.RegisterKeyword("state", targetedStateReader =>
				warToReturn.TargetedState = targetedStateReader.GetString()
			);
			wargoalParser.IgnoreAndLogUnregisteredItems();
			wargoalParser.ParseStream(reader);
		});
		parser.RegisterKeyword("previous", reader => warToReturn.Previous = reader.GetBool());
		parser.IgnoreAndStoreUnregisteredItems(IgnoredTokens);
	}
	public static War Parse(BufferedReader reader) {
		warToReturn = new War();
		parser.ParseStream(reader);
		if (!warToReturn.Previous && warToReturn.AttackerCountryIds.Count == 0) {
			throw new System.FormatException("War has no attackers");
		}
		if (!warToReturn.Previous && warToReturn.DefenderCountryIds.Count == 0) {
			throw new System.FormatException("War has no defenders!");
		}
		return warToReturn;
	}

	// Wargoal types seem to be hardcoded, they don't need to be loaded from game files.
	private const string WargoalTypeRegex = "take_province|naval_superiority|superiority|enforce_military_access|independence";

	private static readonly Parser parser = new();
	private static War warToReturn = new();
	public static IgnoredKeywordsSet IgnoredTokens { get; } = new();
}