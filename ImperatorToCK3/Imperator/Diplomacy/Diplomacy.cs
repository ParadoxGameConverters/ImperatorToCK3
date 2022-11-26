using commonItems;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.Linq;

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
				if (war.AttackerCountryIds.Count == 0) {
					Logger.Debug($"War started at {war.StartDate} has no attackers!");
					return;
				}
				if (war.DefenderCountryIds.Count == 0) {
					Logger.Debug($"War started at {war.StartDate} has no defenders!");
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

		if (War.IgnoredTokens.Any()) {
			Logger.Debug($"Ignored War tokens: {War.IgnoredTokens}");
		}
		if (ignoredDatabaseTokens.Count > 0) {
			Logger.Debug($"Ignored Diplomacy database tokens: {ignoredDatabaseTokens}");
		}
		if (ignoredTokens.Any()) {
			Logger.Debug($"Ignored Diplomacy tokens: {ignoredTokens}");
		}
		Logger.Info($"Loaded {Wars.Count} wars.");
	}

	private readonly IgnoredKeywordsSet ignoredTokens = new();
	private readonly IgnoredKeywordsSet ignoredDatabaseTokens = new();
}