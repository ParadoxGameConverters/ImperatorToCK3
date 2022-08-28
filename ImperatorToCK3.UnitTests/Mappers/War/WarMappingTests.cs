using ImperatorToCK3.Mappers.War;
using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.War {
	public class WarMappingTests {
		[Fact]
		public void FieldsDefaultToEmptyAndNull() {
			var reader = new BufferedReader("={}");
			var mapping = WarMapping.Parse(reader);
			Assert.Empty(mapping.ImperatorWarGoals);
			Assert.Null(mapping.CK3CasusBelli);
		}
		[Fact]
		public void FieldsCanBeSet() {
			var reader = new BufferedReader("= { ck3 = ck3CB imp = goal1 imp = goal2 }");
			var mapping = WarMapping.Parse(reader);
			Assert.Collection(mapping.ImperatorWarGoals,
				item => Assert.Equal("goal1", item),
				item => Assert.Equal("goal2", item)
			);
			Assert.Equal("ck3CB", mapping.CK3CasusBelli);
		}
	}
}
