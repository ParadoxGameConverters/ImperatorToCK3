using ImperatorToCK3.CK3.Titles;
using Xunit;
using commonItems;

namespace ImperatorToCK3.UnitTests.CK3.Titles {
	public class TitleHistoryTests {
		[Fact]
		public void HolderDefaultsToZeroString() {
			var history = new TitleHistory();

			Assert.Equal("0", history.GetHolderId(new Date(867, 1, 1)));
		}

		[Fact]
		public void LiegeDefaultsToNullopt() {
			var history = new TitleHistory();

			Assert.Null(history.Liege);
		}

		[Fact]
		public void GovernmentDefaultsToNull() {
			var history = new TitleHistory();

			Assert.Null(history.GetGovernment(new Date(867, 1, 1)));
		}

		[Fact]
		public void DevelopmentLevelDefaultsToNull() {
			var history = new TitleHistory();

			Assert.Null(history.DevelopmentLevel);
		}

		[Fact]
		public void HistoryCanBeLoadedFromStream() {
			var titlesHistory = new TitlesHistory("TestFiles/title_history", new Date(867, 1, 1));
			var history = titlesHistory.PopTitleHistory("k_rome");

			Assert.Equal("67", history.History.GetSimpleFieldValue("holder", new Date(867, 1, 1)));
			Assert.Equal("e_italia", history.Liege);
		}

		[Fact]
		public void HistoryIsLoadedFromDatedBlocks() {
			var titlesHistory = new TitlesHistory("TestFiles/title_history", new Date(867, 1, 1));
			var history = titlesHistory.PopTitleHistory("k_greece");

			Assert.Equal("420", history.History.GetSimpleFieldValue("holder", new Date(867, 1, 1)));
			Assert.Equal(20, history.DevelopmentLevel);
		}
	}
}
