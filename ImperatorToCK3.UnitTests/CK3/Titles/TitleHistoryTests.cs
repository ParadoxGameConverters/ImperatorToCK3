using commonItems;
using ImperatorToCK3.CK3.Titles;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Titles {
	public class TitleHistoryTests {
		[Fact]
		public void HolderDefaultsToZeroString() {
			var history = new TitleHistory();

			Assert.Equal("0", history.GetHolderId(new Date(867, 1, 1)));
		}

		[Fact]
		public void LiegeDefaultsToNull() {
			var history = new TitleHistory();
			Assert.Null(history.GetLiege(new Date(867, 1, 1)));
		}

		[Fact]
		public void GovernmentDefaultsToNull() {
			var history = new TitleHistory();

			Assert.Null(history.GetGovernment(new Date(867, 1, 1)));
		}

		[Fact]
		public void DevelopmentLevelDefaultsToNull() {
			var history = new TitleHistory();

			Assert.Null(history.GetDevelopmentLevel(new Date(867, 1, 1)));
		}

		[Fact]
		public void HistoryCanBeLoadedFromStream() {
			var date = new Date(867, 1, 1);
			var titlesHistory = new TitlesHistory("TestFiles/title_history");
			var history = titlesHistory.PopTitleHistory("k_rome");

			Assert.NotNull(history);
			Assert.Equal("67", history.GetHolderId(date));
			Assert.Equal("e_italia", history.GetLiege(date));
		}

		[Fact]
		public void HistoryIsLoadedFromDatedBlocks() {
			var date = new Date(867, 1, 1);
			var titlesHistory = new TitlesHistory("TestFiles/title_history");
			var history = titlesHistory.PopTitleHistory("k_greece");

			Assert.NotNull(history);
			Assert.Equal("420", history.GetHolderId(date));
			Assert.Equal(20, history.GetDevelopmentLevel(date));
		}
	}
}
