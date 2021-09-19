using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Imperator.Countries;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.TagTitle {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class TagTitleMapperTests {
		const string tagTitleMappingsPath = "TestFiles/configurables/title_map.txt";
		const string governorshipTitleMappingsPath = "TestFiles/configurables/governorMappings.txt";

		[Fact]
		public void TitleCanBeMatchedFromTag() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath); // reads title_map.txt from TestFiles
			var match = mapper.GetTitleForTag("CRT", CountryRank.majorPower);

			Assert.Equal("k_krete", match);
		}

		[Fact]
		public void TitleCanBeMatchedByRanklessLink() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath); // reads title_map.txt from TestFiles
			var match = mapper.GetTitleForTag("RAN", CountryRank.majorPower);

			Assert.Equal("d_rankless", match);
		}

		[Fact]
		public void TitleCanBeGeneratedFromTag() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath);
			var match = mapper.GetTitleForTag("ROM", CountryRank.localPower, "Rome");
			var match2 = mapper.GetTitleForTag("DRE", CountryRank.localPower, "Dre Empire");

			Assert.Equal("k_IMPTOCK3_ROM", match);
			Assert.Equal("e_IMPTOCK3_DRE", match2);
		}
		[Fact]
		public void TitleCanBeGeneratedFromGovernorship() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath);
			var match = mapper.GetTitleForGovernorship("apulia_region", "ROM", "e_roman_empire");
			var match2 = mapper.GetTitleForGovernorship("pepe_region", "DRE", "k_dre_empire");

			Assert.Equal("k_IMPTOCK3_ROM_apulia_region", match);
			Assert.Equal("d_IMPTOCK3_DRE_pepe_region", match2);
		}

		[Fact]
		public void GetTitleForTagReturnsNullOnEmptyParameter() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath);
			var match = mapper.GetTitleForTag("", CountryRank.migrantHorde, "");

			Assert.Null(match);
		}
		[Fact]
		public void GetTitleGovernorshipTagReturnsNullOnEmptyParameter() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath);
			var match = mapper.GetTitleForGovernorship("", "", "");

			Assert.Null(match);
		}

		[Fact]
		public void TagCanBeRegistered() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath);
			mapper.RegisterTag("BOR", "e_boredom");
			var match = mapper.GetTitleForTag("BOR", CountryRank.localPower);

			Assert.Equal("e_boredom", match);
		}
		[Fact]
		public void GovernorshipCanBeRegistered() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath);
			mapper.RegisterGovernorship("aquitaine_region", "BOR", "k_atlantis");
			var match = mapper.GetTitleForGovernorship("aquitaine_region", "BOR", "e_roman_empire");

			Assert.Equal("k_atlantis", match);
		}
	}
}
