using commonItems;
using Xunit;
using ImperatorToCK3.Mappers.Localization;

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

			Assert.Equal("someName", countryName.BaseName.Name);
			Assert.Equal("someAdjective", countryName.BaseName.GetAdjective());
			Assert.Null(countryName.BaseName.BaseName);
		}

		[Fact]
		public void AdjLocBlockDefaultsToNullopt() {
			var reader = new BufferedReader(string.Empty);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			var locMapper = new LocalizationMapper();
			Assert.Null(countryName.GetAdjectiveLocBlock(locMapper, new()));
		}

		[Fact]
		public void AdjLocBlockReturnsCorrectLocForRevolts() {
			var reader = new BufferedReader(
				"adjective = CIVILWAR_FACTION_ADJECTIVE \n base = { name = someName adjective = someAdjective }"
			);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			var locMapper = new LocalizationMapper();
			locMapper.AddLocalization("CIVILWAR_FACTION_ADJECTIVE", new LocBlock { english = "$ADJ$" });
			locMapper.AddLocalization("someAdjective", new LocBlock { english = "Roman" });
			Assert.Equal("Roman", countryName.GetAdjectiveLocBlock(locMapper, new()).english);
		}

		[Fact]
		public void NameLocBlockDefaultsToNullopt() {
			var reader = new BufferedReader(string.Empty);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			var locMapper = new LocalizationMapper();
			Assert.Null(countryName.GetNameLocBlock(locMapper, new()));
		}

		[Fact]
		public void NameLocBlockReturnsCorrectLocForRevolts() {
			var reader = new BufferedReader(
				"name = CIVILWAR_FACTION_NAME\n base = { name = someName adjective = someAdjective }"
			);
			var countryName = ImperatorToCK3.Imperator.Countries.CountryName.Parse(reader);

			var locMapper = new LocalizationMapper();
			locMapper.AddLocalization("CIVILWAR_FACTION_NAME", new LocBlock { english = "$ADJ$ Revolt" });
			locMapper.AddLocalization("someAdjective", new LocBlock { english = "Roman" });
			Assert.Equal("Roman Revolt", countryName.GetNameLocBlock(locMapper, new()).english);
		}
	}
}
