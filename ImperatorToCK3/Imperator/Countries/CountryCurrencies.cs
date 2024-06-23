using commonItems;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.Imperator.Countries;

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
		RegisterKeyword("manpower", reader => Manpower = reader.GetDouble());
		RegisterKeyword("gold", reader => Gold = reader.GetDouble());
		RegisterKeyword("stability", reader => Stability = reader.GetDouble());
		RegisterKeyword("tyranny", reader => Tyranny = reader.GetDouble());
		RegisterKeyword("war_exhaustion", reader => WarExhaustion = reader.GetDouble());
		RegisterKeyword("aggressive_expansion", reader => AggressiveExpansion = reader.GetDouble());
		RegisterKeyword("political_influence", reader => PoliticalInfluence = reader.GetDouble());
		RegisterKeyword("military_experience", reader => MilitaryExperience = reader.GetDouble());
		RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
			IgnoredTokens.Add(token);
			ParserHelpers.IgnoreItem(reader);
		});
	}
	public static ConcurrentIgnoredKeywordsSet IgnoredTokens { get; } = [];
}