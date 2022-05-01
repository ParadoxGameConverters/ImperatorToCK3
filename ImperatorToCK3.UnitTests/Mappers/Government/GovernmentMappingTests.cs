using commonItems;
using ImperatorToCK3.Mappers.Government;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Government {
	public class GovernmentMappingTests {
		[Fact]
		public void EverythingDefaultsToEmpty() {
			var reader = new BufferedReader("={}");
			var mapping = new GovernmentMapping(reader);
			Assert.True(string.IsNullOrEmpty(mapping.Ck3Government));
			Assert.Empty(mapping.ImperatorGovernments);
		}
		[Fact]
		public void Ck3GovernmentCanBeSet() {
			var reader = new BufferedReader("= { ck3 = ck3Government }");
			var mapping = new GovernmentMapping(reader);
			Assert.Equal("ck3Government", mapping.Ck3Government);
		}
		[Fact]
		public void ImperatorGovernmentsCanBeSet() {
			var reader = new BufferedReader("= { imp = gov1 imp = gov2 }");
			var mapping = new GovernmentMapping(reader);
			Assert.Collection(mapping.ImperatorGovernments,
				item => Assert.Equal("gov1", item),
				item => Assert.Equal("gov2", item)
			);
		}
	}
}
