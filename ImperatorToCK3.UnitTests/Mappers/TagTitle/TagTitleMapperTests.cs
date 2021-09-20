using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Imperator.Countries;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.TagTitle {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class TagTitleMapperTests {
		[Fact]
		public void TitleCanBeMatched() {
			var mapper = new TagTitleMapper("TestFiles/configurables/title_map.txt"); // reads title_map.txt from TestFiles
			var match = mapper.GetTitleForTag("CRT", CountryRank.majorPower);

			Assert.Equal("k_krete", match);
		}

		[Fact]
		public void TitleCanBeMatchedByRanklessLink() {
			var mapper = new TagTitleMapper("TestFiles/configurables/title_map.txt"); // reads title_map.txt from TestFiles
			var match = mapper.GetTitleForTag("RAN", CountryRank.majorPower);

			Assert.Equal("d_rankless", match);
		}

		[Fact]
		public void TitleCanBeGenerated() {
			var mapper = new TagTitleMapper("TestFiles/configurables/title_map.txt");
			var match = mapper.GetTitleForTag("ROM", CountryRank.localPower, "Rome");
			var match2 = mapper.GetTitleForTag("DRE", CountryRank.localPower, "Dre Empire");

			Assert.Equal("k_IMPTOCK3_ROM", match);
			Assert.Equal("e_IMPTOCK3_DRE", match2);
		}

		[Fact]
		public void GetTitleForTagReturnsNulloptOnEmptyParameter() {
			var mapper = new TagTitleMapper("TestFiles/configurables/title_map.txt");
			var match = mapper.GetTitleForTag("", CountryRank.migrantHorde, "");

			Assert.Null(match);
		}

		[Fact]
		public void TagCanBeRegistered() {
			var mapper = new TagTitleMapper("TestFiles/configurables/title_map.txt");
			mapper.RegisterTag("BOR", "e_boredom");
			var match = mapper.GetTitleForTag("BOR", CountryRank.localPower);

			Assert.Equal("e_boredom", match);
		}
	}
}
