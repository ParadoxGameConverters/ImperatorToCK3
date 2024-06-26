using commonItems;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Diplomacy;

public sealed class DiplomacyDB {
	private readonly List<War> wars = [];
	public IReadOnlyList<War> Wars => wars;
	
	private readonly List<Dependency> dependencies = [];
	public IReadOnlyList<Dependency> Dependencies => dependencies;
	
	public DiplomacyDB(BufferedReader diplomacyReader) {
		var parser = new Parser();
		parser.RegisterKeyword("database", databaseReader => {
			var databaseParser = new Parser();
			databaseParser.RegisterRegex(CommonRegexes.Integer, LoadWar);
			databaseParser.IgnoreAndStoreUnregisteredItems(ignoredDatabaseTokens);

			databaseParser.ParseStream(databaseReader);
		});
		parser.RegisterKeyword("dependency", LoadDependency);
		parser.IgnoreAndStoreUnregisteredItems(ignoredTokens);

		parser.ParseStream(diplomacyReader);

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

	private void LoadWar(BufferedReader warReader, string warId) {
		var war = War.Parse(warReader);
		if (war.Previous) { // no need to import old wars
			return;
		}
		if (war.AttackerCountryIds.Count == 0) {
			Logger.Debug($"Skipping war {warId} has no attackers!");
			return;
		}
		if (war.DefenderCountryIds.Count == 0) {
			Logger.Debug($"Skipping war {warId} has no defenders!");
			return;
		}
		if (war.WarGoal is null) {
			Logger.Warn($"Skipping war {warId} with no wargoal!");
			return;
		}
		wars.Add(war);
	}

	private void LoadDependency(BufferedReader dependencyReader) {
		// Variables can be initialized with any values, they will be overwritten by the parser.
		ulong overlordId = 0;
		ulong subjectId = 0;
		Date startDate = new("1.1.1", AUC: true);
		string subjectType = string.Empty;
		
		var dependencyParser = new Parser();
		dependencyParser.RegisterKeyword("first", reader => overlordId = reader.GetULong());
		dependencyParser.RegisterKeyword("second", reader => subjectId = reader.GetULong());
		dependencyParser.RegisterKeyword("start_date", reader => {
			startDate = new Date(reader.GetString(), AUC: true);
		});
		dependencyParser.RegisterKeyword("subject_type", reader => subjectType = reader.GetString());
		dependencyParser.RegisterKeyword("seed", ParserHelpers.IgnoreItem);
		dependencyParser.IgnoreAndLogUnregisteredItems();
		
		dependencyParser.ParseStream(dependencyReader);
		dependencies.Add(new(overlordId, subjectId, startDate, subjectType));
	}

	private readonly IgnoredKeywordsSet ignoredTokens = new();
	private readonly IgnoredKeywordsSet ignoredDatabaseTokens = new();
}