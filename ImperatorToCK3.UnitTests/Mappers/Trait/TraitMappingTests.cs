using commonItems;
using ImperatorToCK3.Mappers.Trait;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Trait {
	public class TraitMappingTests {
		[Fact]
		public void FieldsDefaultToEmptyAndNull() {
			var reader = new BufferedReader("={}");
			var mapping = new TraitMapping(reader);
			Assert.Empty(mapping.ImpTraits);
			Assert.Null(mapping.Ck3Trait);
		}
		[Fact]
		public void FieldsCanBeSet() {
			var reader = new BufferedReader("= { ck3 = ck3Trait imp = trait1 imp = trait2 }");
			var mapping = new TraitMapping(reader);
			Assert.Collection(mapping.ImpTraits,
				item => Assert.Equal("trait1", item),
				item => Assert.Equal("trait2", item)
			);
			Assert.Equal("ck3Trait", mapping.Ck3Trait);
		}
	}
}
