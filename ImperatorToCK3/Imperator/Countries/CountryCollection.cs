using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Families;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.Countries;

public class CountryCollection : IdObjectCollection<ulong, Country> {
	public CountryCollection() { }

	public void LoadCountriesFromBloc(BufferedReader reader) {
		var blocParser = new Parser();
		blocParser.RegisterKeyword("country_database", LoadCountries);
		blocParser.IgnoreAndLogUnregisteredItems();
		blocParser.ParseStream(reader);

		Logger.Debug($"Ignored CountryCurrencies tokens: {CountryCurrencies.IgnoredTokens}");
		Logger.Debug($"Ignored RulerTerm tokens: {RulerTerm.IgnoredTokens}");
		Logger.Debug($"Ignored Country tokens: {Country.IgnoredTokens}");
	}
	public void LoadCountries(BufferedReader reader) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);

		foreach (var country in this) {
			country.LinkOriginCountry(this);
		}
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterRegex(CommonRegexes.Integer, (reader, countryId) => {
			var newCountry = Country.Parse(reader, ulong.Parse(countryId));
			Add(newCountry);
		});
		parser.IgnoreAndLogUnregisteredItems();
	}

	public void LinkFamilies(FamilyCollection families) {
		SortedSet<ulong> idsWithoutDefinition = new();
		var counter = this.Sum(country => country.LinkFamilies(families, idsWithoutDefinition));

		if (idsWithoutDefinition.Count > 0) {
			Logger.Debug($"Families without definition: {string.Join(", ", idsWithoutDefinition)}");
		}

		Logger.Info($"{counter} families linked to countries.");
	}
}