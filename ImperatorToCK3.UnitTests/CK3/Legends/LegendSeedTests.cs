using commonItems;
using ImperatorToCK3.CK3.Legends;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Legends;

public class LegendSeedTests {
	[Fact]
	public void IdIsStored() {
		var seed = new LegendSeed("legend_1", new BufferedReader("{}"));
		Assert.Equal("legend_1", seed.Id);
	}

	[Fact]
	public void Serialize_ReturnsBodyString() {
		// The body is parsed as a StringOfItem; it should round-trip via Serialize.
		var seed = new LegendSeed("legend_2", new BufferedReader("{ foo = bar }"));
		Assert.Equal("{ foo = bar }", seed.Serialize(string.Empty, withBraces: true));
	}
}
