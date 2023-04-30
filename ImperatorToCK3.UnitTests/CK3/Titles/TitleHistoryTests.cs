using commonItems;
using ImperatorToCK3.CK3.Titles;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Titles;

public class TitleHistoryTests {
	[Fact]
	public void HolderDefaultsToZeroString() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("k_title");

		Assert.Equal("0", title.GetHolderId(new Date(867, 1, 1)));
	}

	[Fact]
	public void LiegeDefaultsToNull() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("k_title");

		Assert.Null(title.GetLiege(new Date(867, 1, 1)));
	}

	[Fact]
	public void GovernmentDefaultsToNull() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("k_title");

		Assert.Null(title.GetGovernment(new Date(867, 1, 1)));
	}

	[Fact]
	public void DevelopmentLevelDefaultsToNull() {
		var titles = new Title.LandedTitles();
		var title = titles.Add("k_title");

		Assert.Null(title.GetDevelopmentLevel(new Date(867, 1, 1)));
	}
}