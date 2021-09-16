using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Imperator.Diplomacy {
	public class War {
		public Date StartDate { get; private set; } = new(1, 1, 1);
		public List<ulong> AttackerCountryIds { get; } = new();
		public List<ulong> DefenderCountryIds { get; } = new();
		public string WarGoal { get; private set; } = "raiding_wargoal";

		static War() {
			parser.RegisterKeyword("start_date", reader => {
				var dateStr = ParserHelpers.GetString(reader);
				warToReturn.StartDate = new Date(dateStr, AUC: true);
			});
			parser.RegisterKeyword("attacker", reader => {
				warToReturn.AttackerCountryIds.Add(ParserHelpers.GetULong(reader));
			});
			parser.RegisterKeyword("defender", reader => {
				warToReturn.DefenderCountryIds.Add(ParserHelpers.GetULong(reader));
			});
			parser.RegisterRegex(wargoalTypeRegex, reader => {
				var typeParser = new Parser();
				typeParser.RegisterKeyword("type", reader =>
					warToReturn.WarGoal = ParserHelpers.GetString(reader)
				);
				typeParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
				typeParser.ParseStream(reader);
			});
			// todo: check "previous" keyword
			parser.RegisterRegex(CommonRegexes.Catchall, (reader, keyword) => {
				IgnoredTokens.Add(keyword);
				ParserHelpers.IgnoreItem(reader);
			});
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

		private readonly static Parser parser = new();
		private static War warToReturn = new();
		public static List<string> IgnoredTokens { get; } = new();
	}
}
