using commonItems;
using commonItems.Localization;
using FluentAssertions;
using ImperatorToCK3.Imperator;
using ImperatorToCK3.Imperator.Armies;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Armies;

public class UnitCollectionTests {
	[Fact]
	public void SubunitsCanBeLoaded() {
		var unitCollection = new UnitCollection();

		var reader = new BufferedReader(@"1={} 2={} 3=none 1040187400={}");
		unitCollection.LoadSubunits(reader);

		unitCollection.Subunits
			.Select(unit => unit.Id)
			.Should().BeEquivalentTo(new ulong[] {1, 2, 1040187400});
	}

	[Fact]
	public void UnitsCanBeLoaded() {
		var unitCollection = new UnitCollection();

		var reader = new BufferedReader(@"1={} 2={} 3=none 1040187400={}");
		unitCollection.LoadUnits(reader, new LocDB("english"), new Defines());

		unitCollection
			.Select(unit => unit.Id)
			.Should().BeEquivalentTo(new ulong[] {1, 2, 1040187400});
	}
}