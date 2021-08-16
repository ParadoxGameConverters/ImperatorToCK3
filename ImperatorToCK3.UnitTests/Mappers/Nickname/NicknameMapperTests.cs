using Xunit;
using commonItems;
using ImperatorToCK3.Mappers.Nickname;

namespace ImperatorToCK3.UnitTests.Mappers.Nickname {
	public class NicknameMapperTests {
		[Fact]
		public void nonMatchGivesEmptyOptional() {
			var reader = new BufferedReader("link = { ck3 = ck3Nickname imp = impNickname }");
			var mapper = new NicknameMapper(reader);

			var ck3Nickname = mapper.GetCK3NicknameForImperatorNickname("nonMatchingNickname");
			Assert.Null(ck3Nickname);
		}


		[Fact]
		public void ck3NicknameCanBeFound() {
			var reader = new BufferedReader("link = { ck3 = ck3Nickname imp = impNickname }");
			var mapper = new NicknameMapper(reader);

			var ck3Nickname = mapper.GetCK3NicknameForImperatorNickname("impNickname");
			Assert.Equal("ck3Nickname", ck3Nickname);
		}


		[Fact]
		public void multipleImpNicknamesCanBeInARule() {
			var reader = new BufferedReader("link = { ck3 = ck3Nickname imp = impNickname imp = impNickname2 }");
			var mapper = new NicknameMapper(reader);

			var ck3Nickname = mapper.GetCK3NicknameForImperatorNickname("impNickname2");
			Assert.Equal("ck3Nickname", ck3Nickname);
		}


		[Fact]
		public void correctRuleMatches() {
			var reader = new BufferedReader(
				"link = { ck3 = ck3Nickname imp = impNickname }" +
				"link = { ck3 = ck3Nickname2 imp = impNickname2 }"
			);
			var mapper = new NicknameMapper(reader);

			var ck3Nickname = mapper.GetCK3NicknameForImperatorNickname("impNickname2");
			Assert.Equal("ck3Nickname2", ck3Nickname);
		}
	}
}
