using commonItems;
using ImperatorToCK3.CK3.Religions;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions; 

public class ReligionTests {
	[Fact]
	public void FaithsAreLoaded() {
		var reader = new BufferedReader("{ faiths={ orthodox={} catholic={} } }");
		var religion = new Religion("christianity", reader);
		
		Assert.Collection(religion.Faiths,
			faith=>Assert.Equal("orthodox", faith.Id),
			faith=>Assert.Equal("catholic", faith.Id));
	}
}