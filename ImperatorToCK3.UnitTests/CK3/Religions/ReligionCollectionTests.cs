using ImperatorToCK3.CK3.Religions;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions; 

public class ReligionCollectionTests {
	[Fact]
	public void ReligionsAreGroupedByFile() {
		var religions = new ReligionCollection();
		religions.LoadReligions("TestFiles/CK3/game/common/religion/religions");
		
		Assert.Collection(religions.ReligionsPerFile["religion_a.txt"],
			religion=>Assert.Equal("religion_a", religion.Id));
		Assert.Collection(religions.ReligionsPerFile["multiple_religions.txt"],
			religion=>Assert.Equal("religion_b", religion.Id),
			religion=>Assert.Equal("religion_c", religion.Id));
	}
}