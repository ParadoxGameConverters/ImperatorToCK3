using commonItems;
using ImperatorToCK3.Mappers.Trait;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Trait;

public class TraitMappingTests {
	[Fact]
	public void FieldsDefaultToEmptyAndNull() {
		var reader = new BufferedReader("={}");
		var mapping = new TraitMapping(reader);
		Assert.Empty(mapping.ImperatorTraits);
		Assert.Null(mapping.CK3Trait);
	}
	[Fact]
	public void FieldsCanBeSet() {
		var reader = new BufferedReader("= { ck3=ck3Trait ir=trait1 ir=trait2 }");
		var mapping = new TraitMapping(reader);
		Assert.Collection(mapping.ImperatorTraits,
			item => Assert.Equal("trait1", item),
			item => Assert.Equal("trait2", item)
		);
		Assert.Equal("ck3Trait", mapping.CK3Trait);
	}
}