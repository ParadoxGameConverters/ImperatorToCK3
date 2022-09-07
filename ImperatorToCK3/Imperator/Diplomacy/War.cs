using commonItems;
using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Diplomacy; 

public class War {
	public Date StartDate { get; private set; } = new(1, 1, 1);
	public List<ulong> AttackerCountryIds { get; } = new();
	public List<ulong> DefenderCountryIds { get; } = new();
	public string? WarGoal { get; private set; }

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
		parser.RegisterRegex(wargoalTypeRegex, reader => {
			var typeParser = new Parser();
			typeParser.RegisterKeyword("type", typeReader =>
				warToReturn.WarGoal = typeReader.GetString()
			);
			typeParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			typeParser.ParseStream(reader);
		});
		// TODO: check "previous" keyword
		parser.IgnoreAndStoreUnregisteredItems(IgnoredTokens);
	}
	public static War Parse(BufferedReader reader) {
		warToReturn = new War();
		parser.ParseStream(reader);
		if (warToReturn.AttackerCountryIds.Count == 0) {
			throw new System.FormatException("War has no attackers");
		}
		if (warToReturn.DefenderCountryIds.Count == 0) {
			throw new System.FormatException("War has no defenders!");
		}
		return warToReturn;
	}

	// Wargoal types seem to be hardcoded, they don't need to be loaded from game files.
	private const string wargoalTypeRegex = "take_province|naval_superiority|superiority|enforce_military_access|independence";

	private static readonly Parser parser = new();
	private static War warToReturn = new();
	public static OrderedSet<string> IgnoredTokens { get; } = new();
}