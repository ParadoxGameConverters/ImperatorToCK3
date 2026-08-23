using ImperatorToCK3.CK3.Titles;
using System;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Titles;

public class TitleRankUtilsTests {
	[Theory]
	[InlineData('h', TitleRank.hegemony)]
	[InlineData('e', TitleRank.empire)]
	[InlineData('k', TitleRank.kingdom)]
	[InlineData('d', TitleRank.duchy)]
	[InlineData('c', TitleRank.county)]
	[InlineData('b', TitleRank.barony)]
	public void CharToTitleRankReturnsCorrectTitleRank(char c, TitleRank expectedRank) {
		var rank = TitleRankUtils.CharToTitleRank(c);
		Assert.Equal(expectedRank, rank);
	}
	
	[Fact]
	public void CharToTitleRankThrowsOnUnknownRank() {
		Assert.Throws<ArgumentOutOfRangeException>(() => TitleRankUtils.CharToTitleRank('x'));
	}
}