using commonItems;
using ImperatorToCK3.CommonUtils;
using System.Collections.Concurrent;
using System.Linq;

namespace ImperatorToCK3.Imperator.Countries;

public class RulerTerm {
	public class PreImperatorRulerInfo {
		public string? Name { get; set; }
		public Date? BirthDate { get; set; }
		public Date? DeathDate { get; set; }
		public string? Religion { get; set; }
		public string? Culture { get; set; }
		public string? Nickname { get; set; }
		public Country? Country { get; set; }
	}
	public ulong? CharacterId { get; private set; }
	public Date StartDate { get; private set; } = new();
	public string? Government { get; private set; }
	public PreImperatorRulerInfo? PreImperatorRuler { get; set; }

	public static RulerTerm Parse(BufferedReader reader) {
		var newTerm = new RulerTerm();

		var parser = new Parser();
		parser.RegisterKeyword("character", r => newTerm.CharacterId = r.GetULong());
		parser.RegisterKeyword("start_date", r => {
			var dateString = r.GetString();
			newTerm.StartDate = new Date(dateString, AUC: true);
		});
		parser.RegisterKeyword("government", r => newTerm.Government = r.GetString());
		parser.RegisterRegex(CommonRegexes.Catchall, (r, token) => {
			IgnoredTokens.Add(token);
			ParserHelpers.IgnoreItem(r);
		});
		parser.ParseStream(reader);
		
		return newTerm;
	}

	public static readonly ConcurrentIgnoredKeywordsSet IgnoredTokens = [];

	public RulerTerm() { }
	public RulerTerm(BufferedReader prehistoryRulerReader, CountryCollection countries) {
		PreImperatorRuler = new PreImperatorRulerInfo();
		var prehistoryParser = new Parser();

		prehistoryParser.RegisterKeyword("name", reader => PreImperatorRuler.Name = reader.GetString());
		prehistoryParser.RegisterKeyword("birth_date", reader => {
			var dateStr = reader.GetString();
			PreImperatorRuler.BirthDate = new Date(dateStr, AUC: true);
		});
		prehistoryParser.RegisterKeyword("death_date", reader => {
			var dateStr = reader.GetString();
			PreImperatorRuler.DeathDate = new Date(dateStr, AUC: true);
		});
		prehistoryParser.RegisterKeyword("throne_date", reader => {
			var dateStr = reader.GetString();
			StartDate = new Date(dateStr, AUC: true);
		});
		prehistoryParser.RegisterKeyword("religion", reader => PreImperatorRuler.Religion = reader.GetString());
		prehistoryParser.RegisterKeyword("culture", reader => PreImperatorRuler.Culture = reader.GetString());
		prehistoryParser.RegisterKeyword("nickname", reader => PreImperatorRuler.Nickname = reader.GetString());
		prehistoryParser.RegisterKeyword("country", reader => {
			var tag = reader.GetString();
			if (tagToCountryCache.TryGetValue(tag, out var cachedCountry)) {
				PreImperatorRuler.Country = cachedCountry;
			} else {
				var matchingCountries = countries.Where(c => c.Tag == tag).ToArray();
				if (matchingCountries.Length != 1) {
					Logger.Warn($"Pre-Imperator ruler has wrong tag: {tag}!");
					return;
				}
				var countryId = matchingCountries[0].Id;
				PreImperatorRuler.Country = countries[countryId];
				tagToCountryCache[tag] = PreImperatorRuler.Country;
			}
		});

		prehistoryParser.ParseStream(prehistoryRulerReader);
	}
	private static readonly ConcurrentDictionary<string, Country> tagToCountryCache = new();
}