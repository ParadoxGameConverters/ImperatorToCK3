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
		public PreImperatorRulerInfo? PreImperatorRuler { get; set; }

		public static RulerTerm Parse(BufferedReader reader) {
			parsedTerm = new RulerTerm();
			parser.ParseStream(reader);
			return parsedTerm;
		}

		public static readonly HashSet<string> IgnoredTokens = new();

		private static readonly Parser parser = new();
		private static RulerTerm parsedTerm = new();
		static RulerTerm() {
			parser.RegisterKeyword("character", reader => parsedTerm.CharacterId = reader.GetULong());
			parser.RegisterKeyword("start_date", reader => {
				var dateString = reader.GetString();
				parsedTerm.StartDate = new Date(dateString, AUC: true);
			});
			parser.RegisterKeyword("government", reader => parsedTerm.Government = reader.GetString());
			parser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				IgnoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});
		}

		public RulerTerm() { }
		public RulerTerm(BufferedReader prehistoryRulerReader, Countries countries) {
			PreImperatorRuler = new();
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
					var matchingCountries = countries.Values.Where(c => c.Tag == tag).ToArray();
					if (matchingCountries.Length != 1) {
						Logger.Warn($"Pre-Imperator ruler has wrong tag: {tag}!");
						return;
					}
					var countryId = matchingCountries[0].Id;
					PreImperatorRuler.Country = countries[countryId];
					tagToCountryCache.Add(tag, PreImperatorRuler.Country);
				}
			});

			prehistoryParser.ParseStream(prehistoryRulerReader);
		}
		private static readonly Dictionary<string, Country> tagToCountryCache = new();
	}
}
