using commonItems;
using commonItems.Localization;
using AwesomeAssertions;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.Imperator;
using ImperatorToCK3.Imperator.Armies;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Characters;

public class CharacterCollectionLegionTests {
	[Fact]
	public void LegionGroupingIndexesOnlyArmyLegionsByCountry() {
		var unitCollection = new UnitCollection();
		var locDB = new LocDB("english");
		var defines = new ImperatorDefines();

		unitCollection.AddOrReplace(new Unit(1, new BufferedReader("country=1 legion={} is_army=yes"), unitCollection, locDB, defines));
		unitCollection.AddOrReplace(new Unit(2, new BufferedReader("country=1 is_army=yes"), unitCollection, locDB, defines));
		unitCollection.AddOrReplace(new Unit(3, new BufferedReader("country=2 legion={} is_army=yes"), unitCollection, locDB, defines));
		unitCollection.AddOrReplace(new Unit(4, new BufferedReader("country=2 legion={} is_army=no"), unitCollection, locDB, defines));

		var legionsByCountry = CharacterCollection.GetLegionsByCountry(unitCollection);

		legionsByCountry.Keys.Should().BeEquivalentTo(new ulong[] {1, 2});
		legionsByCountry[1].Select(unit => unit.Id).Should().BeEquivalentTo(new ulong[] {1});
		legionsByCountry[2].Select(unit => unit.Id).Should().BeEquivalentTo(new ulong[] {3});
	}
}