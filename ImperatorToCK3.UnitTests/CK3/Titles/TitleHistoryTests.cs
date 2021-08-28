using ImperatorToCK3.CK3.Titles;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Titles {
	public class TitleHistoryTests {
		[Fact]
		public void HolderDefaultsToZeroString() {
			var history = new TitleHistory();

			Assert.Equal("0", history.Holder);
		}

		[Fact]
		public void LiegeDefaultsToNullopt() {
			var history = new TitleHistory();

			Assert.Null(history.Liege);
		}

		[Fact]
		public void GovernmentDefaultsToNull() {
			var history = new TitleHistory();

			Assert.Null(history.Government);
		}

		[Fact]
		public void DevelopmentLevelDefaultsToNull() {
			var history = new TitleHistory();

			Assert.Null(history.DevelopmentLevel);
		}

		[Fact]
		public void HistoryCanBeLoadedFromStream() {
			var titlesHistory = new TitlesHistory("TestFiles/title_history");
			var history = titlesHistory.PopTitleHistory("k_rome");

			Assert.Equal("67", history.Holder);
			Assert.Equal("e_italia", history.Liege);
		}

		[Fact]
		public void HistoryIsLoadedFromDatedBlocks() {
			var titlesHistory = new TitlesHistory("TestFiles/title_history");
			var history = titlesHistory.PopTitleHistory("k_greece");

			Assert.Equal("420", history.Holder);
			Assert.Equal(20, history.DevelopmentLevel);
		}
	}
}
