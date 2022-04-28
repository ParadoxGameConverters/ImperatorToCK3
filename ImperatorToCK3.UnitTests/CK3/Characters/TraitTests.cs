using commonItems;
using ImperatorToCK3.CK3.Characters;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Characters;

public class TraitTests {
	[Fact]
	public void OppositesDefaultToEmpty() {
		var trait = new Trait("dumb");
		Assert.Empty(trait.Opposites);
	}
	[Fact]
	public void OppositesAreRead() {
		var reader = new BufferedReader("{ opposites = { wise smart } }");
		var trait = new Trait("dumb", reader);

		Assert.Collection(trait.Opposites,
			opposite => Assert.Equal("wise", opposite),
			opposite => Assert.Equal("smart", opposite)
		);
	}
}