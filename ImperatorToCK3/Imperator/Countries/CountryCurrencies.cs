using commonItems;

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
			RegisterKeyword("manpower", reader => {
				Manpower = new SingleDouble(reader).Double;
			});
			RegisterKeyword("gold", reader => {
				Gold = new SingleDouble(reader).Double;
			});
			RegisterKeyword("stability", reader => {
				Stability = new SingleDouble(reader).Double;
			});
			RegisterKeyword("tyranny", reader => {
				Tyranny = new SingleDouble(reader).Double;
			});
			RegisterKeyword("war_exhaustion", reader => {
				WarExhaustion = new SingleDouble(reader).Double;
			});
			RegisterKeyword("aggressive_expansion", reader => {
				AggressiveExpansion = new SingleDouble(reader).Double;
			});
			RegisterKeyword("political_influence", reader => {
				PoliticalInfluence = new SingleDouble(reader).Double;
			});
			RegisterKeyword("military_experience", reader => {
				MilitaryExperience = new SingleDouble(reader).Double;
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
	}
}
