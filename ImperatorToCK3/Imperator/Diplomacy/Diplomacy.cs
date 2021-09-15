using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Imperator.Diplomacy {
	public class Diplomacy {
		public List<War> Wars { get; } = new();
		public Diplomacy(BufferedReader reader) {
			var parser = new Parser();
			parser.RegisterRegex(CommonRegexes.Integer, (reader, warId) => {
				var war = War.Parse(reader);
				if (war.WarGoal is null) {
					Logger.Warn($"Skipping war {warId} with no wargoal!");
				}
				Wars.Add(war);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				ignoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});

			parser.ParseStream(reader);
			Logger.Debug("Ignored War tokens: " + string.Join(", ", War.IgnoredTokens));
			Logger.Debug("Ignored Diplomacy tokens: " + string.Join(", ", ignoredTokens));
			Logger.Info($"Loaded {Wars.Count} wars.");
		}

		private readonly List<string> ignoredTokens = new();
	}
}
