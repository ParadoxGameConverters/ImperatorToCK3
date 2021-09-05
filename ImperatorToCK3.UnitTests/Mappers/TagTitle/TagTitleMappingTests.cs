using ImperatorToCK3.Mappers.TagTitle;
using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.TagTitle {
	public class TagTitleMappingTests {
		[Fact]
		public void SimpleTagMatch() {
			var reader = new BufferedReader("{ ck3 = e_roman_empire imp = ROM }");
			var mapping = TagTitleMapping.Parse(reader);
			var match = mapping.TagRankMatch("ROM", "");

			Assert.Equal("e_roman_empire", match);
		}

		[Fact]
		public void SimpleTagMatchFailsOnWrongTag() {
			var reader = new BufferedReader("{ ck3 = e_roman_empire imp = REM }");
			var mapping = TagTitleMapping.Parse(reader);
			var match = mapping.TagRankMatch("ROM", "");

			Assert.Null(match);
		}

		[Fact]
		public void SimpleTagMatchFailsOnNoTag() {
			var reader = new BufferedReader("{ ck3 = e_roman_empire }");
			var mapping = TagTitleMapping.Parse(reader);
			var match = mapping.TagRankMatch("ROM", "");

			Assert.Null(match);
		}

		[Fact]
		public void TagRankMatch() {
			var reader = new BufferedReader("{ ck3 = e_roman_empire imp = ROM rank = e }");
			var mapping = TagTitleMapping.Parse(reader);
			var match = mapping.TagRankMatch("ROM", "e");

			Assert.Equal("e_roman_empire", match);
		}

		[Fact]
		public void TagRankMatchFailsOnWrongRank() {
			var reader = new BufferedReader("{ ck3 = e_roman_empire imp = ROM rank = k }");
			var mapping = TagTitleMapping.Parse(reader);
			var match = mapping.TagRankMatch("ROM", "e");

			Assert.Null(match);
		}

		[Fact]
		public void TagRankMatchSucceedsOnNoRank() {
			var reader = new BufferedReader("{ ck3 = e_roman_empire imp = ROM }");
			var mapping = TagTitleMapping.Parse(reader);
			var match = mapping.TagRankMatch("ROM", "e");

			Assert.Equal("e_roman_empire", match);
		}
	}
}
