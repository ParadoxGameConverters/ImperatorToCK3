using commonItems;
using commonItems.Localization;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Countries {
	public class CountryNameTests {
		[Fact]
		public void NameDefaultsToEmpty() {
			var reader = new BufferedReader(string.Empty);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			Assert.Empty(countryName.Name);
		}

		[Fact]
		public void NameCanBeSet() {
			var reader = new BufferedReader(
				"name = someName adjective = someAdjective"
			);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			Assert.Equal("someName", countryName.Name);
		}

		[Fact]
		public void AdjectiveDefaultsTo_ADJ() {
			var reader = new BufferedReader(string.Empty);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			Assert.Equal("_ADJ", countryName.GetAdjective());
		}

		[Fact]
		public void AdjectiveCanBeSet() {
			var reader = new BufferedReader(
				"name = someName adjective = someAdjective"
			);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			Assert.Equal("someAdjective", countryName.GetAdjective());
		}

		[Fact]
		public void BaseDefaultsToNullptr() {
			var reader = new BufferedReader(string.Empty);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			Assert.Null(countryName.BaseName);
		}

		[Fact]
		public void BaseCanBeSet() {
			var reader = new BufferedReader(
				"name = revolt\n base = { name = someName adjective = someAdjective }"
			);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			Assert.Equal("someName", countryName.BaseName!.Name);
			Assert.Equal("someAdjective", countryName.BaseName.GetAdjective());
			Assert.Null(countryName.BaseName.BaseName);
		}

		[Fact]
		public void AdjLocBlockDefaultsToNull() {
			var reader = new BufferedReader(string.Empty);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			var locDB = new LocDB("english");
			Assert.Null(countryName.GetAdjectiveLocBlock(locDB, new()));
		}

		[Fact]
		public void AdjLocBlockReturnsCorrectLocForRevolts() {
			var reader = new BufferedReader(
				"adjective = CIVILWAR_FACTION_ADJECTIVE \n base = { name = someName adjective = someAdjective }"
			);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			var locDB = new LocDB("english");
			var locBlock1 = locDB.AddLocBlock("CIVILWAR_FACTION_ADJECTIVE");
			locBlock1["english"] = "$ADJ$";
			var locBlock2 = locDB.AddLocBlock("someAdjective");
			locBlock2["english"] = "Roman";
			Assert.Equal("Roman", countryName.GetAdjectiveLocBlock(locDB, new())!["english"]);
		}

		[Fact]
		public void NameLocBlockDefaultsToNull() {
			var reader = new BufferedReader(string.Empty);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			var locDB = new LocDB("english");
			Assert.Null(countryName.GetNameLocBlock(locDB, new()));
		}

		[Fact]
		public void NameLocBlockReturnsCorrectLocForRevolts() {
			var reader = new BufferedReader(
				"name = CIVILWAR_FACTION_NAME\n base = { name = someName adjective = someAdjective }"
			);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			var locDB = new LocDB("english");
			var locBlock1 = locDB.AddLocBlock("CIVILWAR_FACTION_NAME");
			locBlock1["english"] = "$ADJ$ Revolt";
			var locBlock2 = locDB.AddLocBlock("someAdjective");
			locBlock2["english"] = "Roman";
			Assert.Equal("Roman Revolt", countryName.GetNameLocBlock(locDB, new())!["english"]);
		}
	}
}
