using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Countries {
	public class Countries : Dictionary<ulong, Country> {
		public Countries() { }
		public Countries(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);
		}
		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, countryId) => {
				var newCountry = Country.Parse(reader, ulong.Parse(countryId));
				Add(newCountry.Id, newCountry);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public void LinkFamilies(Families.Families families) {
			SortedSet<ulong> idsWithoutDefinition = new();
			var counter = Values.Sum(country => country.LinkFamilies(families, idsWithoutDefinition));

			if (idsWithoutDefinition.Count > 0) {
				Logger.Debug($"Families without definition: {string.Join(", ", idsWithoutDefinition)}");
			}

			Logger.Info($"{counter} families linked to countries.");
		}
		public static Countries ParseBloc(BufferedReader reader) {
			var blocParser = new Parser();
			Countries countries = new();
			blocParser.RegisterKeyword("country_database", reader =>
				countries = new Countries(reader)
			);
			blocParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			blocParser.ParseStream(reader);
			blocParser.ClearRegisteredRules();
			Logger.Debug($"Ignored CountryCurrencies tokens: {string.Join(", ", CountryCurrencies.IgnoredTokens)}");
			Logger.Debug($"Ignored RulerTerm tokens: {string.Join(", ", RulerTerm.IgnoredTokens)}");
			Logger.Debug($"Ignored Country tokens: {string.Join(", ", Country.IgnoredTokens)}");
			return countries;
		}
	}
}
