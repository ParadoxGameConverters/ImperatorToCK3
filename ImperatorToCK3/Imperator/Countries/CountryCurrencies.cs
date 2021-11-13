using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Countries {
	public class CountryCurrencies : Parser {
		public double Manpower { get; private set; } = 0;
		public double Gold { get; private set; } = 0;
		public double Stability { get; private set; } = 50;
		public double Tyranny { get; private set; } = 0;
		public double WarExhaustion { get; private set; } = 0;
		public double AggressiveExpansion { get; private set; } = 0;
		public double PoliticalInfluence { get; private set; } = 0;
		public double MilitaryExperience { get; private set; } = 0;

		public CountryCurrencies() { }
		public CountryCurrencies(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}
		private void RegisterKeys() {
			RegisterKeyword("manpower", reader =>
				Manpower = ParserHelpers.GetDouble(reader)
			);
			RegisterKeyword("gold", reader =>
				Gold = ParserHelpers.GetDouble(reader)
			);
			RegisterKeyword("stability", reader =>
				Stability = ParserHelpers.GetDouble(reader)
			);
			RegisterKeyword("tyranny", reader =>
				Tyranny = ParserHelpers.GetDouble(reader)
			);
			RegisterKeyword("war_exhaustion", reader =>
				WarExhaustion = ParserHelpers.GetDouble(reader)
			);
			RegisterKeyword("aggressive_expansion", reader =>
				AggressiveExpansion = ParserHelpers.GetDouble(reader)
			);
			RegisterKeyword("political_influence", reader =>
				PoliticalInfluence = ParserHelpers.GetDouble(reader)
			);
			RegisterKeyword("military_experience", reader =>
				MilitaryExperience = ParserHelpers.GetDouble(reader)
			);
			RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				IgnoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});
		}
		public static HashSet<string> IgnoredTokens { get; } = new();
	}
}
