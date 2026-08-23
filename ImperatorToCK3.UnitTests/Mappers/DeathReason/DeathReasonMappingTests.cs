using commonItems;
using ImperatorToCK3.Mappers.DeathReason;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.DeathReason;

public class DeathReasonMappingTests {
	[Fact]
	public void CK3ReasonDefaultsToNull() {
		var reader = new BufferedReader("");
		var mapping = new DeathReasonMapping(reader);
		Assert.Null(mapping.Ck3Reason);
	}
	[Fact]
	public void CK3ReasonCanBeSet() {
		var reader = new BufferedReader("= { ck3 = ck3Trait }");
		var mapping = new DeathReasonMapping(reader);
		Assert.Equal("ck3Trait", mapping.Ck3Reason);
	}
	[Fact]
	public void ImperatorReasonsDefaultToEmpty() {
		var reader = new BufferedReader("");
		var mapping = new DeathReasonMapping(reader);
		Assert.Empty(mapping.ImperatorReasons);
	}
	[Fact]
	public void ImperatorReasonsCanBeSet() {
		var reader = new BufferedReader("= { ir=reason_dumb ir=reason_bear }");
		var mapping = new DeathReasonMapping(reader);
		Assert.Collection(mapping.ImperatorReasons,
			item => Assert.Equal("reason_bear", item),
			item => Assert.Equal("reason_dumb", item)
		);
	}
}