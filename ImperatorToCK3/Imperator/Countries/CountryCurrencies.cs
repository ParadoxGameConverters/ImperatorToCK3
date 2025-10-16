using commonItems;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.Imperator.Countries;

internal sealed class CountryCurrencies : Parser {
	public float Manpower { get; private set; } = 0;
	public float Gold { get; private set; } = 0;
	public float Stability { get; private set; } = 50;
	public float Tyranny { get; private set; } = 0;
	public float WarExhaustion { get; private set; } = 0;
	public float AggressiveExpansion { get; private set; } = 0;
	public float PoliticalInfluence { get; private set; } = 0;
	public float MilitaryExperience { get; private set; } = 0;

	public CountryCurrencies() { }
	public CountryCurrencies(BufferedReader reader) {
		RegisterKeys();
		ParseStream(reader);
		ClearRegisteredRules();
	}
	private void RegisterKeys() {
		RegisterKeyword("manpower", reader => Manpower = reader.GetFloat());
		RegisterKeyword("gold", reader => Gold = reader.GetFloat());
		RegisterKeyword("stability", reader => Stability = reader.GetFloat());
		RegisterKeyword("tyranny", reader => Tyranny = reader.GetFloat());
		RegisterKeyword("war_exhaustion", reader => WarExhaustion = reader.GetFloat());
		RegisterKeyword("aggressive_expansion", reader => AggressiveExpansion = reader.GetFloat());
		RegisterKeyword("political_influence", reader => PoliticalInfluence = reader.GetFloat());
		RegisterKeyword("military_experience", reader => MilitaryExperience = reader.GetFloat());
		RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
			IgnoredTokens.Add(token);
			ParserHelpers.IgnoreItem(reader);
		});
	}
	public static ConcurrentIgnoredKeywordsSet IgnoredTokens { get; } = [];
}