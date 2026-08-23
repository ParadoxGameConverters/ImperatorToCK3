using commonItems;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Countries;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CountriesTests {
	[Fact]
	public void CountriesDefaultToEmpty() {
		var reader = new BufferedReader(
			"= { }"
		);
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		countries.LoadCountriesFromBloc(reader);

		Assert.Empty(countries);
	}

	[Fact]
	public void CountriesCanBeLoaded() {
		var reader = new BufferedReader(
			"= {\n" +
			"42={}\n" +
			"43={}\n" +
			"}"
		);
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		countries.LoadCountries(reader);

		Assert.Collection(countries,
			item => Assert.Equal((ulong)42, item.Id),
			item => Assert.Equal((ulong)43, item.Id));
	}

	[Fact]
	public void FamilyCanBeLinked() {
		var reader = new BufferedReader(
			"={42={family=8}}\n"
		);
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		countries.LoadCountries(reader);

		var reader2 = new BufferedReader(
			"8={key=\"Cornelli\" prestige=2 member={ 4479 4480}}\n"
		);
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		families.LoadFamilies(reader2);
		countries.LinkFamilies(families);

		var country = countries[42];
		var family = country.Families[8];
		Assert.Equal(2, family.Prestige);
	}

	[Fact]
	public void MultipleFamiliesCanBeLinked() {
		var reader = new BufferedReader(
			"={\n" +
			"43={ family = 10}\n" +
			"42={family=8}\n" +
			"44={minor_family= 9}\n" +
			"}\n"
		);
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		countries.LoadCountries(reader);

		var reader2 = new BufferedReader(
			"={\n" +
			"8={key=\"Cornelli\" prestige=2 member={ 4479 4480} }\n" +
			"9={key=\"minor_bmb\" prestige=69 minor_family=yes member={ 4479 4480} }\n" +
			"10={key=\"minor_rom\" prestige=7 minor_family=yes member={ 69 420} }\n" +
			"}\n"
		);
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		families.LoadFamilies(reader2);
		countries.LinkFamilies(families);

		var country = countries[42];
		var family = country.Families[8];

		var country2 = countries[43];
		var family2 = country2.Families[10];

		var country3 = countries[44];
		var family3 = country3.Families[9];

		Assert.Equal(2, family.Prestige);
		Assert.Equal(7, family2.Prestige);
		Assert.Equal(69, family3.Prestige);
	}

	[Fact]
	public void BrokenLinkAttemptThrowsWarning() {
		var reader = new BufferedReader(
			"={\n" +
			"42={ family = 8 }\n" +
			"44={ minor_family = 10 }\n" + // no pop 10
			"}\n"
		);
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		countries.LoadCountries(reader);

		var reader2 = new BufferedReader(
			"={\n" +
			"8={key=\"Cornelli\" prestige=0 member={ 4479 4480}}\n" +
			"9={key=\"minor_bmb\" prestige=0 minor_family=yes member={ 4479 4480}}\n" +
			"}\n"
		);
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		families.LoadFamilies(reader2);

		var output = new StringWriter();
		Console.SetOut(output);

		countries.LinkFamilies(families);

		Assert.Contains("[DEBUG] Families without definition: 10", output.ToString());
	}

	[Fact]
	public void AllCountryTypesCanBeSet() {
		var reader = new BufferedReader(
			"={\n" +
			"1={country_type=real}\n" +
			"2={country_type=rebels}\n" +
			"3={country_type=pirates}\n" +
			"4={country_type=barbarians}\n" +
			"5={country_type=mercenaries}\n" +
			"}\n"
		);
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		countries.LoadCountries(reader);

		Assert.Equal(ImperatorToCK3.Imperator.Countries.CountryType.real, countries[1].CountryType);
		Assert.Equal(ImperatorToCK3.Imperator.Countries.CountryType.rebels, countries[2].CountryType);
		Assert.Equal(ImperatorToCK3.Imperator.Countries.CountryType.pirates, countries[3].CountryType);
		Assert.Equal(ImperatorToCK3.Imperator.Countries.CountryType.barbarians, countries[4].CountryType);
		Assert.Equal(ImperatorToCK3.Imperator.Countries.CountryType.mercenaries, countries[5].CountryType);
	}

	[Fact]
	public void UnrecognizedCountryTypeDefaultsToReal() {
		var output = new StringWriter();
		Console.SetOut(output);

		var reader = new BufferedReader(
			"={42={country_type=aliens}}\n"
		);
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		countries.LoadCountries(reader);

		Assert.Equal(ImperatorToCK3.Imperator.Countries.CountryType.real, countries[42].CountryType);
		Assert.Contains("[ERROR] Unrecognized country type: aliens, defaulting to real.", output.ToString());
	}
}