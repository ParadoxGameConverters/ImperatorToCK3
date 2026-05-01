using AwesomeAssertions;
using ImperatorToCK3.CK3;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Diplomacy;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3;

public sealed class WorldLookupTests {
	[Fact]
	public void FirstValueIndexKeepsFirstCountyLevelEntryPerCountry() {
		var country1 = new Country(1);
		var country2 = new Country(2);
		var firstDependency = new Dependency(10, 1, new commonItems.Date(1, 1, 1), "tributary");
		var secondDependency = new Dependency(11, 1, new commonItems.Date(2, 1, 1), "vassal");
		var countyLevelCountries = new List<KeyValuePair<Country, Dependency?>> {
			new(country1, firstDependency),
			new(country1, secondDependency),
			new(country2, null)
		};

		var indexed = World.GetFirstValuesByKey(countyLevelCountries, entry => entry.Key.Id);

		indexed.Should().HaveCount(2);
		indexed[1].Value.Should().BeSameAs(firstDependency);
		indexed[2].Value.Should().BeNull();
	}
}