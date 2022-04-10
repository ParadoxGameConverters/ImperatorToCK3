using commonItems;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Provinces;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Countries {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class CountryTests {
		[Fact]
		public void FieldsDefaultToCorrectValues() {
			var reader = new BufferedReader(string.Empty);
			var country = Country.Parse(reader, 42);
			Assert.Equal(string.Empty, country.Tag);
			Assert.Equal(string.Empty, country.HistoricalTag);
			Assert.Equal(string.Empty, country.Name);
			Assert.Null(country.Capital);
			Assert.Equal(0, country.Currencies.Manpower);
			Assert.Equal(0, country.Currencies.Gold);
			Assert.Equal(50, country.Currencies.Stability);
			Assert.Equal(0, country.Currencies.Tyranny);
			Assert.Equal(0, country.Currencies.WarExhaustion);
			Assert.Equal(0, country.Currencies.AggressiveExpansion);
			Assert.Equal(0, country.Currencies.PoliticalInfluence);
			Assert.Equal(0, country.Currencies.MilitaryExperience);
			Assert.Null(country.Monarch);
			Assert.Null(country.Color1);
			Assert.Null(country.Color2);
			Assert.Null(country.Color3);
			Assert.Empty(country.GetLaws());
			Assert.False(country.PlayerCountry);
			Assert.Null(country.Government);
			Assert.Equal(GovernmentType.monarchy, country.GovernmentType);
		}
		[Fact]
		public void FieldsCanBeSet() {
			var reader = new BufferedReader(
				"= {\n" +
				"\ttag=\"WTF\"" +
				"\thistorical=\"WTF\"" +
				"\tcountry_name = {\n" +
				"\t\tname=\"WTF\"\n" +
				"\t}\n" +
				"\tflag=\"WTF\"" +
				"\tcapital = 32\n" +
				"\tcurrency_data={ manpower=1 gold=2 stability=69 tyranny=4 war_exhaustion=2 aggressive_expansion=50 political_influence=4 military_experience=1}" +
				"\tmonarch=69" +
				"\tprimary_culture=athenian" +
				"\treligion=hellenic" +
				"\tcolor = rgb { 1 2 3 }" +
				"\tcolor2 = rgb { 4 5 6 }" +
				"\tcolor3 = rgb { 7 8 9 }" +
				"\tgovernment_key = dictatorship" +
				"}"
			);
			var country = Country.Parse(reader, 42);
			Assert.Equal((ulong)42, country.Id);
			Assert.Equal("WTF", country.Tag);
			Assert.Equal("WTF", country.HistoricalTag);
			Assert.Equal("WTF", country.Name);
			Assert.Equal("WTF", country.Flag);
			Assert.Equal((ulong)32, country.Capital);
			Assert.Equal(1, country.Currencies.Manpower);
			Assert.Equal(2, country.Currencies.Gold);
			Assert.Equal(69, country.Currencies.Stability);
			Assert.Equal(4, country.Currencies.Tyranny);
			Assert.Equal(2, country.Currencies.WarExhaustion);
			Assert.Equal(50, country.Currencies.AggressiveExpansion);
			Assert.Equal(4, country.Currencies.PoliticalInfluence);
			Assert.Equal(1, country.Currencies.MilitaryExperience);
			Assert.Null(country.Monarch); // not linked yet
			Assert.Equal("athenian", country.PrimaryCulture);
			Assert.Equal("hellenic", country.Religion);
			Assert.Equal(new Color(1, 2, 3), country.Color1);
			Assert.Equal(new Color(4, 5, 6), country.Color2);
			Assert.Equal(new Color(7, 8, 9), country.Color3);
			Assert.Equal("dictatorship", country.Government);
			Assert.Equal(GovernmentType.monarchy, country.GovernmentType);

			var countries = new CountryCollection { country };

			var monarch = ImperatorToCK3.Imperator.Characters.Character.Parse(
				new BufferedReader("{ country=42 }"),
				"69",
				null
			);
			monarch.LinkCountry(countries);
			Assert.NotNull(country.Monarch);
			Assert.Equal((ulong)69, country.Monarch.Id);
		}

		[Fact]
		public void MultipleCountriesCanShareHistoricalTag() {
			var countriesReader = new BufferedReader(
				"1 = {\n" +
				"\ttag=\"AAA\"" +
				"\thistorical=\"AAA\"" +
				"}\n" +
				"2 = {\n" +
				"\ttag=\"BBB\"" +
				"\thistorical=\"AAA\"" +
				"\torigin=1" +
				"}\n" +
				"3 = {\n" +
				"\ttag=\"CCC\"" +
				"\thistorical=\"AAA\"" +
				"\torigin=2" +
				"}\n" +
				"4 = {\n" +
				"\ttag=\"DDD\"" +
				"\thistorical=\"AAA\"" +
				"\torigin=3" +
				"}\n"
			);
			var countries = new CountryCollection(countriesReader);
			Assert.Equal("AAA", countries[1].HistoricalTag);
			Assert.Equal("AAA", countries[2].HistoricalTag);
			Assert.Equal("AAA", countries[3].HistoricalTag);
			Assert.Equal("AAA", countries[4].HistoricalTag);
		}

		[Fact]
		public void CorrectGovernmentTypeIsRecognized() {
			var config = new Configuration {
				CK3Path = "TestFiles/CK3"
			};
			var mods = new List<Mod> {
				new("cool_mod", Path.Combine(Directory.GetCurrentDirectory(), "TestFiles/documents/CK3/mod/cool_mod"))
			};
			Country.LoadGovernments(config, mods);

			var monarchyReader = new BufferedReader("government_key = super_monarchy");
			var monarchyCountry = Country.Parse(monarchyReader, 1);
			Assert.Equal(GovernmentType.monarchy, monarchyCountry.GovernmentType);

			var republicReader = new BufferedReader("government_key = aristocratic_republic");
			var republicCountry = Country.Parse(republicReader, 2);
			Assert.Equal(GovernmentType.republic, republicCountry.GovernmentType);

			var tribalReader = new BufferedReader("government_key = tribal_federation");
			var tribalCountry = Country.Parse(tribalReader, 3);
			Assert.Equal(GovernmentType.tribal, tribalCountry.GovernmentType);
		}
		[Fact]
		public void CorrectCountryRankIsReturned() {
			var reader = new BufferedReader(string.Empty);
			var country1 = Country.Parse(reader, 1);

			var country2 = Country.Parse(reader, 2);
			country2.RegisterProvince(new Province(0));

			var country3 = Country.Parse(reader, 3);
			for (ulong i = 0; i < 4; ++i) {
				country3.RegisterProvince(new Province(i));
			}

			var country4 = Country.Parse(reader, 4);
			for (ulong i = 0; i < 25; ++i) {
				country4.RegisterProvince(new Province(i));
			}

			var country5 = Country.Parse(reader, 5);
			for (ulong i = 0; i < 200; ++i) {
				country5.RegisterProvince(new Province(i));
			}

			var country6 = Country.Parse(reader, 6);
			for (ulong i = 0; i < 600; ++i) {
				country6.RegisterProvince(new Province(i));
			}

			Assert.Equal(CountryRank.migrantHorde, country1.Rank);
			Assert.Equal(CountryRank.cityState, country2.Rank);
			Assert.Equal(CountryRank.localPower, country3.Rank);
			Assert.Equal(CountryRank.regionalPower, country4.Rank);
			Assert.Equal(CountryRank.majorPower, country5.Rank);
			Assert.Equal(CountryRank.greatPower, country6.Rank);
		}

		[Fact]
		public void OnlyLawsForCorrectGovernmentTypeAreReturned() {
			var reader = new BufferedReader(
				"= {\n" +
				"\tsuccession_law = lawA\n" +
				"\ttribal_authority_laws = lawB\n" + // won't be returned, law is for tribals
				"\trepublican_mediterranean_laws = lawC\n" + // won't be returned, law is for republics
				"\tmonarchy_legitimacy_laws = lawD\n" +
				"}"
			);
			// gov type is monarchy by default
			var country = Country.Parse(reader, 42);
			Assert.Collection(country.GetLaws(),
				item => Assert.Equal("lawA", item),
				item => Assert.Equal("lawD", item)
			);
		}

		[Fact]
		public void WrongTypeLawsAreNotSet() {
			var reader = new BufferedReader(
				"= {\n" +
				"\tnonexistent_law_type_laws = lawA\n" +
				"}"
			);
			// gov type is monarchy by default
			var country = Country.Parse(reader, 42);
			Assert.Empty(country.GetLaws());
		}

		[Fact]
		public void IgnoredTokensAreSaved() {
			var reader1 = new BufferedReader("= { monarch=20 ignoredKeyword1=something ignoredKeyword2={} }");
			var reader2 = new BufferedReader("= { ignoredKeyword1=stuff ignoredKeyword3=stuff }");
			Country.IgnoredTokens.Clear();
			_ = Country.Parse(reader1, 1);
			_ = Country.Parse(reader2, 2);

			var expectedIgnoredTokens = new HashSet<string> {
				"ignoredKeyword1", "ignoredKeyword2", "ignoredKeyword3"
			};
			Assert.True(Country.IgnoredTokens.SetEquals(expectedIgnoredTokens));
		}

		[Fact]
		public void IgnoredCountryCurrenciesTokensAreSaved() {
			var reader = new BufferedReader(
				"= { currency_data={ manpower=1 innovations=0 } }"
			);
			var reader2 = new BufferedReader(
				"= { currency_data={ ignoredKeyword1=stuff ignoredKeyword2=stuff } }"
			);
			_ = Country.Parse(reader, 1);
			_ = Country.Parse(reader2, 2);

			var expectedIgnoredTokens = new HashSet<string> {
				"ignoredKeyword1", "ignoredKeyword2", "innovations"
			};
			Assert.True(CountryCurrencies.IgnoredTokens.SetEquals(expectedIgnoredTokens));
		}
	}
}
