using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Imperator.Countries {
	public class RulerTerm {
		public ulong? CharacterId { get; private set; }
		public Date StartDate { get; private set; } = new();
		public string? Government { get; private set; }

		public static RulerTerm Parse(BufferedReader reader) {
			parsedTerm = new RulerTerm();
			parser.ParseStream(reader);
			return parsedTerm;
		}

		public static readonly HashSet<string> IgnoredTokens = new();
		private static readonly Parser parser = new();
		private static RulerTerm parsedTerm = new();
		static RulerTerm() {
			parser.RegisterKeyword("character", reader => {
				parsedTerm.CharacterId = ParserHelpers.GetULong(reader);
			});
			parser.RegisterKeyword("start_date", reader => {
				var dateString = ParserHelpers.GetString(reader);
				parsedTerm.StartDate = new Date(dateString, AUC: true);
			});
			parser.RegisterKeyword("government", reader => {
				parsedTerm.Government = ParserHelpers.GetString(reader);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				IgnoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});
		}
	}
}
