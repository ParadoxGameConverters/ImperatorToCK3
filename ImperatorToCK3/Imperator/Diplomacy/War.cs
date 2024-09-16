using commonItems;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Diplomacy;

public sealed class War {
	public Date StartDate { get; private set; } = new(1, 1, 1);
	public bool Previous { get; private set; }
	public IList<ulong> AttackerCountryIds { get; } = new List<ulong>();
	public IList<ulong> DefenderCountryIds { get; } = new List<ulong>();
	public string? WarGoal { get; private set; }
	public ulong? TargetedStateId { get; private set; }

	static War() {
		parser.RegisterKeyword("start_date", reader => {
			var dateStr = reader.GetString();
			warToReturn.StartDate = new Date(dateStr, AUC: true);
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
				warToReturn.TargetedStateId = targetedStateReader.GetULong()
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
		return warToReturn;
	}

	// Wargoal types seem to be hardcoded, they don't need to be loaded from game files.
	private const string WargoalTypeRegex = "take_province|naval_superiority|superiority|enforce_military_access|independence";

	private static readonly Parser parser = new();
	private static War warToReturn = new();
	public static IgnoredKeywordsSet IgnoredTokens { get; } = new();
}