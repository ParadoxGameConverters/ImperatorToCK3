using ImperatorToCK3.CK3.Cultures;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Cultures; 

public class PillarTests {
	[Fact]
	public void PillarIsCorrectlyInitialized() {
		var pillar = new Pillar("test_pillar", new PillarData { Type = "test_type" });
		Assert.Equal("test_pillar", pillar.Id);
		Assert.Equal("test_type", pillar.Type);
	}
}