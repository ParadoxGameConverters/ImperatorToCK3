using ImperatorToCK3.Imperator.Characters;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Characters {
	public class AccessoryGeneDataTests {
		[Fact]
		public void MembersDefaultToEmptyStrings() {
			var data = new AccessoryGeneData();
			Assert.Empty(data.geneName);
			Assert.Empty(data.geneTemplate);
			Assert.Empty(data.objectName);
		}
	}
}
