using commonItems;
using ImperatorToCK3.Mappers.Nickname;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Nickname {
	public class NicknameMappingTests {
		[Fact]
		public void FieldsDefaultToNullAndEmpty() {
			var reader = new BufferedReader("= {}");
			var mapping = new NicknameMapping(reader);
			Assert.Null(mapping.Ck3Nickname);
			Assert.Empty(mapping.ImperatorNicknames);
		}
		[Fact]
		public void FieldsCanBeSet() {
			var reader = new BufferedReader("= { ck3 = ck3Nickname imp = nickname1 imp = nickname2 }");
			var mapping = new NicknameMapping(reader);
			Assert.Equal("ck3Nickname", mapping.Ck3Nickname);
			Assert.Collection(mapping.ImperatorNicknames,
				item => Assert.Equal("nickname1", item),
				item => Assert.Equal("nickname2", item)
			);
		}
	}
}
