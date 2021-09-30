using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Diplomacy {
	public class Diplomacy {
		public List<War> Wars { get; } = new();
		public Diplomacy(BufferedReader reader) {
			var parser = new Parser();
			parser.RegisterKeyword("database", reader => {
				var databaseParser = new Parser();
				databaseParser.RegisterRegex(CommonRegexes.Integer, (reader, warId) => {
					var war = War.Parse(reader);
					if (war.WarGoal is null) {
						Logger.Warn($"Skipping war {warId} with no wargoal!");
					}
					Wars.Add(war);
				});
				databaseParser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
					ignoredDatabaseTokens.Add(token);
					ParserHelpers.IgnoreItem(reader);
				});

				databaseParser.ParseStream(reader);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				ignoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});

			parser.ParseStream(reader);

			Logger.Debug("Ignored War tokens: " + string.Join(", ", War.IgnoredTokens));
			if (ignoredDatabaseTokens.Count > 0) {
				Logger.Debug("Ignored Diplomacy database tokens: " + string.Join(", ", ignoredDatabaseTokens));
			}
			Logger.Debug("Ignored Diplomacy tokens: " + string.Join(", ", ignoredTokens));
			Logger.Info($"Loaded {Wars.Count} wars.");
		}

		private readonly List<string> ignoredTokens = new();
		private readonly List<string> ignoredDatabaseTokens = new();
	}
}
