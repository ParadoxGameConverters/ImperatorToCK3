using commonItems;
using ImperatorToCK3.Mappers.Nickname;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Nickname; 

public class NicknameMapperTests {
	[Fact]
	public void NonMatchGivesNull() {
		var reader = new BufferedReader("link = { ck3 = ck3Nickname imp = impNickname }");
		var mapper = new NicknameMapper(reader);

		var ck3Nickname = mapper.GetCK3NicknameForImperatorNickname("nonMatchingNickname");
		Assert.Null(ck3Nickname);
	}
	[Fact]
	public void NullInNullOut() {
		var reader = new BufferedReader("link = { ck3 = ck3Nickname imp = impNickname }");
		var mapper = new NicknameMapper(reader);

		var match = mapper.GetCK3NicknameForImperatorNickname(null);
		Assert.Null(match);
	}

	[Fact]
	public void Ck3NicknameCanBeFound() {
		var reader = new BufferedReader("link = { ck3 = ck3Nickname imp = impNickname }");
		var mapper = new NicknameMapper(reader);

		var ck3Nickname = mapper.GetCK3NicknameForImperatorNickname("impNickname");
		Assert.Equal("ck3Nickname", ck3Nickname);
	}

	[Fact]
	public void MultipleImpNicknamesCanBeInARule() {
		var reader = new BufferedReader("link = { ck3 = ck3Nickname imp = impNickname imp = impNickname2 }");
		var mapper = new NicknameMapper(reader);

		var ck3Nickname = mapper.GetCK3NicknameForImperatorNickname("impNickname2");
		Assert.Equal("ck3Nickname", ck3Nickname);
	}

	[Fact]
	public void CorrectRuleMatches() {
		var reader = new BufferedReader(
			"link = { ck3 = ck3Nickname imp = impNickname }" +
			"link = { ck3 = ck3Nickname2 imp = impNickname2 }"
		);
		var mapper = new NicknameMapper(reader);

		var ck3Nickname = mapper.GetCK3NicknameForImperatorNickname("impNickname2");
		Assert.Equal("ck3Nickname2", ck3Nickname);
	}

	[Fact]
	public void MappingsAreReadFromFile() {
		var mapper = new NicknameMapper("TestFiles/configurables/nickname_map.txt");
		Assert.Equal("dull", mapper.GetCK3NicknameForImperatorNickname("dull"));
		Assert.Equal("dull", mapper.GetCK3NicknameForImperatorNickname("stupid"));
		Assert.Equal("kind", mapper.GetCK3NicknameForImperatorNickname("friendly"));
		Assert.Equal("brave", mapper.GetCK3NicknameForImperatorNickname("brave"));
	}

	[Fact]
	public void MappingsWithNoCK3NicknameAreIgnored() {
		var reader = new BufferedReader(
			"link = { imp = impNickname }"
		);
		var mapper = new NicknameMapper(reader);

		var ck3Nickname = mapper.GetCK3NicknameForImperatorNickname("impNickname");
		Assert.Null(ck3Nickname);
	}
}