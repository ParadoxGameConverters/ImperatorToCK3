using commonItems;
using ImperatorToCK3.CK3.Religions;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions; 

public class FaithTests {
	[Fact]
	public void HolySitesAreLoaded() {
		var reader = new BufferedReader("{ holy_site=rome holy_site=constantinople holy_site=antioch }");
		var faith = new Faith("chalcedonian", reader);
		
		Assert.Collection(faith.HolySiteIds,
			site=>Assert.Equal("rome", site),
			site=>Assert.Equal("constantinople", site),
			site=>Assert.Equal("antioch", site));
	}
}