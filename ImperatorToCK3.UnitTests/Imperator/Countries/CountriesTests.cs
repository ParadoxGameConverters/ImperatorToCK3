using commonItems;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Countries {
	public class CountriesTests {
		[Fact]
		public void CountriesDefaultToEmpty() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
				"}"
			);
			var countries = new ImperatorToCK3.Imperator.Countries.Countries(reader);

			Assert.Empty(countries.StoredCountries);
		}

		[Fact]
		public void CountriesCanBeLoaded() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
				"42={}\n" +
				"43={}\n" +
				"}"
			);
			var countries = new ImperatorToCK3.Imperator.Countries.Countries(reader);

			Assert.Collection(countries.StoredCountries,
				item => {
					Assert.Equal((ulong)42, item.Key);
					Assert.Equal((ulong)42, item.Value.ID);
				},
				item => {
					Assert.Equal((ulong)43, item.Key);
					Assert.Equal((ulong)43, item.Value.ID);
				}
			);
		}

		[Fact]
		public void FamilyCanBeLinked() {
			var reader = new BufferedReader(
				"={42={family=8}}\n"
			);
			var countries = new ImperatorToCK3.Imperator.Countries.Countries(reader);


			var reader2 = new BufferedReader(
				"8={key=\"Cornelli\" prestige=2 member={ 4479 4480}}\n"
			);
			var families = new ImperatorToCK3.Imperator.Families.Families();
			families.LoadFamilies(reader2);
			countries.LinkFamilies(families);

			var country = countries.StoredCountries[42];
			var family = country.Families[8];

			Assert.NotNull(family);
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
			var countries = new ImperatorToCK3.Imperator.Countries.Countries(reader);

			var reader2 = new BufferedReader(
				"={\n" +
				"8={key=\"Cornelli\" prestige=2 member={ 4479 4480} }\n" +
				"9={key=\"minor_bmb\" prestige=69 minor_family=yes member={ 4479 4480} }\n" +
				"10={key=\"minor_rom\" prestige=7 minor_family=yes member={ 69 420} }\n" +
				"}\n"
			);
			var families = new ImperatorToCK3.Imperator.Families.Families();
			families.LoadFamilies(reader2);
			countries.LinkFamilies(families);


			var country = countries.StoredCountries[42];
			var family = country.Families[8];

			var country2 = countries.StoredCountries[43];
			var family2 = country2.Families[10];

			var country3 = countries.StoredCountries[44];
			var family3 = country3.Families[9];

			Assert.NotNull(family);
			Assert.Equal(2, family.Prestige);
			Assert.NotNull(family2);
			Assert.Equal(7, family2.Prestige);
			Assert.NotNull(family3);
			Assert.Equal(69, family3.Prestige);
		}

		[Fact]
		public void BrokenLinkAttemptThrowsWarning() {
			var reader = new BufferedReader(
				"={\n" +
				"42={ family = 8 }\n" +
				"44={ minor_family = 10 }\n" + /// no pop 10
				"}\n"
			);
			var countries = new ImperatorToCK3.Imperator.Countries.Countries(reader);

			var reader2 = new BufferedReader(
				"={\n" +
				"8={key=\"Cornelli\" prestige=0 member={ 4479 4480}}\n" +
				"9={key=\"minor_bmb\" prestige=0 minor_family=yes member={ 4479 4480}}\n" +
				"}\n"
			);
			var families = new ImperatorToCK3.Imperator.Families.Families();
			families.LoadFamilies(reader2);

			var output = new StringWriter();
			Console.SetOut(output);

			countries.LinkFamilies(families);

			Assert.Contains("[DEBUG] Families without definition: 10", output.ToString());
		}
	}
}
