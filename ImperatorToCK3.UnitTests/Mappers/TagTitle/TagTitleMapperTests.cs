using commonItems;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Mappers.TagTitle;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.TagTitle {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class TagTitleMapperTests {
		private const string tagTitleMappingsPath = "TestFiles/configurables/title_map.txt";
		private const string governorshipTitleMappingsPath = "TestFiles/configurables/governorMappings.txt";

		[Fact]
		public void TitleCanBeMatchedFromTag() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath); // reads title_map.txt from TestFiles
			var country = Country.Parse(new BufferedReader("tag=CRT"), 1);
			for (ulong i = 1; i < 200; ++i) { // makes the country a major power
				var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
				country.RegisterProvince(province);
			}
			var match = mapper.GetTitleForTag(country);

			Assert.Equal("k_krete", match);
		}
		[Fact]
		public void TitleCanBeMatchedFromGovernorship() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath); // reads title_map.txt from TestFiles
			var match = mapper.GetTitleForGovernorship("central_italy_region", "ROM", "e_roman_empire");

			Assert.Equal("k_romagna", match);
		}

		[Fact]
		public void TitleCanBeMatchedByRanklessLink() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath); // reads title_map.txt from TestFiles
			var country = Country.Parse(new BufferedReader("tag=RAN"), 1);
			for (ulong i = 1; i < 200; ++i) { // makes the country a major power
				var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
				country.RegisterProvince(province);
			}
			var match = mapper.GetTitleForTag(country);

			Assert.Equal("d_rankless", match);
		}

		[Fact]
		public void TitleCanBeGeneratedFromTag() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath);
			var rom = Country.Parse(new BufferedReader("tag=ROM"), 1);
			for (ulong i = 0; i < 20; ++i) { // makes the country a local power
				rom.RegisterProvince(new ImperatorToCK3.Imperator.Provinces.Province(i));
			}
			var match = mapper.GetTitleForTag(rom, "Rome");

			var dre = Country.Parse(new BufferedReader("tag=DRE"), 1);
			for (ulong i = 0; i < 20; ++i) { // makes the country a local power
				dre.RegisterProvince(new ImperatorToCK3.Imperator.Provinces.Province(i));
			}
			var match2 = mapper.GetTitleForTag(dre, "Dre Empire");

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
		public void GetTitleForTagReturnsNullOnEmptyTag() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath);
			var country = Country.Parse(new BufferedReader(string.Empty), 1);
			Assert.Empty(country.Tag);
			var match = mapper.GetTitleForTag(country, "");

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
			var country = Country.Parse(new BufferedReader("tag=BOR"), 1);
			for (ulong i = 1; i < 20; ++i) { // makes the country a local power
				var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
				country.RegisterProvince(province);
			}
			var match = mapper.GetTitleForTag(country);

			Assert.Equal("e_boredom", match);
		}
		[Fact]
		public void GovernorshipCanBeRegistered() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath);
			mapper.RegisterGovernorship("aquitaine_region", "BOR", "k_atlantis");
			var match = mapper.GetTitleForGovernorship("aquitaine_region", "BOR", "e_roman_empire");

			Assert.Equal("k_atlantis", match);
		}

		[Fact]
		public void GetCK3TitleRankReturnsCorrectRank() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath);
			var tag1 = Country.Parse(new BufferedReader("tag=TEST_TAG1"), 1);
			for (ulong i = 1; i < 20; ++i) { // makes the country a local power
				var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
				tag1.RegisterProvince(province);
			}
			Assert.Equal('e', mapper.GetTitleForTag(tag1, "Test Empire")[0]);

			var tag2 = Country.Parse(new BufferedReader("tag=TEST_TAG2"), 2);
			for (ulong i = 1; i < 2; ++i) { // makes the country a city state
				var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
				tag2.RegisterProvince(province);
			}
			Assert.Equal('k', mapper.GetTitleForTag(tag2, "Test Kingdom")[0]);

			var tag3 = Country.Parse(new BufferedReader("tag=TEST_TAG3"), 3); // migrant horde
			Assert.Equal('d', mapper.GetTitleForTag(tag3)[0]);

			var tag4 = Country.Parse(new BufferedReader("tag=TEST_TAG4"), 4);
			for (ulong i = 1; i < 2; ++i) { // makes the country a city state
				var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
				tag4.RegisterProvince(province);
			}
			Assert.Equal('d', mapper.GetTitleForTag(tag4)[0]);

			var tag5 = Country.Parse(new BufferedReader("tag=TEST_TAG5"), 5);
			for (ulong i = 1; i < 20; ++i) { // makes the country a local power
				var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
				tag5.RegisterProvince(province);
			}
			Assert.Equal('k', mapper.GetTitleForTag(tag5)[0]);

			var tag6 = Country.Parse(new BufferedReader("tag=TEST_TAG6"), 6);
			for (ulong i = 1; i < 40; ++i) { // makes the country a regional power
				var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
				tag6.RegisterProvince(province);
			}
			Assert.Equal('k', mapper.GetTitleForTag(tag6)[0]);

			var tag7 = Country.Parse(new BufferedReader("tag=TEST_TAG7"), 7);
			for (ulong i = 1; i < 200; ++i) { // makes the country a major power
				var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
				tag7.RegisterProvince(province);
			}
			Assert.Equal('k', mapper.GetTitleForTag(tag7)[0]);

			var tag8 = Country.Parse(new BufferedReader("tag=TEST_TAG8"), 8);
			for (ulong i = 1; i < 501; ++i) { // makes the country a great power
				var province = new ImperatorToCK3.Imperator.Provinces.Province(i);
				tag8.RegisterProvince(province);
			}
			Assert.Equal('e', mapper.GetTitleForTag(tag8)[0]);
		}
	}
}
