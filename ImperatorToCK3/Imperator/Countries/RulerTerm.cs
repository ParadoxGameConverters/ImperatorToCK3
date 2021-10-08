using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Countries {
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
		internal PreImperatorRulerInfo? PreImperatorRuler { get; set; }

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

		public RulerTerm() { }
		public RulerTerm(BufferedReader prehistoryRulerReader, Countries countries) {
			PreImperatorRuler = new();
			var prehistoryParser = new Parser();

			prehistoryParser.RegisterKeyword("name", reader => {
				PreImperatorRuler.Name = ParserHelpers.GetString(reader);
			});
			prehistoryParser.RegisterKeyword("birth_date", reader => {
				var dateStr = ParserHelpers.GetString(reader);
				PreImperatorRuler.BirthDate = new Date(dateStr, AUC: true);
			});
			prehistoryParser.RegisterKeyword("death_date", reader => {
				var dateStr = ParserHelpers.GetString(reader);
				PreImperatorRuler.DeathDate = new Date(dateStr, AUC: true);
			});
			prehistoryParser.RegisterKeyword("throne_date", reader => {
				var dateStr = ParserHelpers.GetString(reader);
				StartDate = new Date(dateStr, AUC: true);
			});
			prehistoryParser.RegisterKeyword("religion", reader => {
				PreImperatorRuler.Religion = ParserHelpers.GetString(reader);
			});
			prehistoryParser.RegisterKeyword("culture", reader => {
				PreImperatorRuler.Culture = ParserHelpers.GetString(reader);
			});
			prehistoryParser.RegisterKeyword("nickname", reader => {
				PreImperatorRuler.Nickname = ParserHelpers.GetString(reader);
			});
			prehistoryParser.RegisterKeyword("country", reader => {
				var tag = ParserHelpers.GetString(reader);
				if (tagToCountryCache.TryGetValue(tag, out var cachedCountry)) {
					PreImperatorRuler.Country = cachedCountry;
				} else {
					var matchingCountries = countries.StoredCountries.Values.Where(c => c.Tag == tag);
					if (matchingCountries.Count() != 1) {
						Logger.Warn($"Pre-Imperator ruler has wrong tag: {tag}!");
						return;
					}
					var countryId = matchingCountries.First().ID;
					PreImperatorRuler.Country = countries.StoredCountries[countryId];
					tagToCountryCache.Add(tag, PreImperatorRuler.Country);
				}
			});

			prehistoryParser.ParseStream(prehistoryRulerReader);
		}
		private static readonly Dictionary<string, Country> tagToCountryCache = new();
	}
}
