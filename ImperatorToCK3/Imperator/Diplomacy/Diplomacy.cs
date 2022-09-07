using commonItems;
using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Diplomacy; 

public class Diplomacy {
	public List<War> Wars { get; } = new();
	public Diplomacy(BufferedReader reader) {
		var parser = new Parser();
		parser.RegisterKeyword("database", databaseReader => {
			var databaseParser = new Parser();
			databaseParser.RegisterRegex(CommonRegexes.Integer, (warReader, warId) => {
				var war = War.Parse(warReader);
				if (war.Previous) { // no need to import old wars
					return;
				}
				if (war.WarGoal is null) {
					Logger.Warn($"Skipping war {warId} with no wargoal!");
					return;
				}
				Wars.Add(war);
			});
			databaseParser.IgnoreAndStoreUnregisteredItems(ignoredDatabaseTokens);

			databaseParser.ParseStream(databaseReader);
		});
		parser.IgnoreAndStoreUnregisteredItems(ignoredTokens);

		parser.ParseStream(reader);

		Logger.Debug($"Ignored War tokens: {string.Join(", ", War.IgnoredTokens)}");
		if (ignoredDatabaseTokens.Count > 0) {
			Logger.Debug($"Ignored Diplomacy database tokens: {string.Join(", ", ignoredDatabaseTokens)}");
		}
		Logger.Debug($"Ignored Diplomacy tokens: {string.Join(", ", ignoredTokens)}");
		Logger.Info($"Loaded {Wars.Count} wars.");
	}

	private readonly OrderedSet<string> ignoredTokens = new();
	private readonly OrderedSet<string> ignoredDatabaseTokens = new();
}