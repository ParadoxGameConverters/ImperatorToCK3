using commonItems;
using ImperatorToCK3.Mappers.Government;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Government;

public class GovernmentMappingTests {
	[Fact]
	public void EverythingDefaultsToEmpty() {
		var reader = new BufferedReader("={}");
		var mapping = new GovernmentMapping(reader);
		Assert.True(string.IsNullOrEmpty(mapping.CK3GovernmentId));
		Assert.Empty(mapping.ImperatorGovernmentIds);
	}
	[Fact]
	public void CK3GovernmentCanBeSet() {
		var reader = new BufferedReader("= { ck3 = ck3Government }");
		var mapping = new GovernmentMapping(reader);
		Assert.Equal("ck3Government", mapping.CK3GovernmentId);
	}
	[Fact]
	public void ImperatorGovernmentsCanBeSet() {
		var reader = new BufferedReader("= { ir = gov1 ir = gov2 }");
		var mapping = new GovernmentMapping(reader);
		Assert.Collection(mapping.ImperatorGovernmentIds,
			item => Assert.Equal("gov1", item),
			item => Assert.Equal("gov2", item)
		);
	}
}