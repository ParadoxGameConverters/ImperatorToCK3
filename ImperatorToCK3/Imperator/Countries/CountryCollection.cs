using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Families;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Countries {
	public class CountryCollection : IdObjectCollection<ulong, Country> {
		public CountryCollection() { }
		public CountryCollection(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);

			Logger.Info("Linking Countries with Countries...");
			LinkCountries();
		}
		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, countryId) => {
				var newCountry = Country.Parse(reader, ulong.Parse(countryId));
				Add(newCountry);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public void LinkFamilies(FamilyCollection families) {
			SortedSet<ulong> idsWithoutDefinition = new();
			var counter = this.Sum(country => country.LinkFamilies(families, idsWithoutDefinition));

			if (idsWithoutDefinition.Count > 0) {
				Logger.Debug($"Families without definition: {string.Join(", ", idsWithoutDefinition)}");
			}

			Logger.Info($"{counter} families linked to countries.");
		}
		private void LinkCountries() {
			SortedSet<ulong> idsWithoutDefinition = new();
			var counter = this.Count(country => country.LinkCountries(this, idsWithoutDefinition));

			if (idsWithoutDefinition.Count > 0) {
				Logger.Debug($"Countries without definition: {string.Join(", ", idsWithoutDefinition)}");
			}

			Logger.Info($"{counter} countries linked to countries.");
		}

		public static CountryCollection ParseBloc(BufferedReader reader) {
			var blocParser = new Parser();
			CountryCollection countries = new();
			blocParser.RegisterKeyword("country_database", reader =>
				countries = new CountryCollection(reader)
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
