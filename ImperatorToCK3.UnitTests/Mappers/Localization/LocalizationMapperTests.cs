using Xunit;
using ImperatorToCK3.Mappers.Localizaton;
using commonItems;

namespace ImperatorToCK3.UnitTests.Mappers.Localization {
    public class LocalizationMapperTests {
		[Fact] public void LocalisationsCanBeLoadedAndMatched() {
			var reader1 = new BufferedReader(
				CommonFunctions.UTF8BOM + "l_english:\n" +
				" key1: \"value 1\" # comment\n" +
				" key2: \"value \"subquoted\" 2\"\n"
			);
			var reader2 = new BufferedReader(
				CommonFunctions.UTF8BOM + "l_french:\n" +
				" key1: \"valuee 1\"\n" +
				" key2: \"valuee \"subquoted\" 2\"\n"
			);
			var reader3 = new BufferedReader(
				CommonFunctions.UTF8BOM + "l_english:\n" +
				" key1: \"replaced value 1\"\n"
			);

			var locs = new LocalizationMapper();
			locs.ScrapeStream(reader1, "english");
			locs.ScrapeStream(reader2, "french");
			locs.ScrapeStream(reader3, "english");

			Assert.Equal("replaced value 1", ((LocBlock)locs.GetLocBlockForKey("key1")).english);
			Assert.Equal("value \"subquoted\" 2", ((LocBlock)locs.GetLocBlockForKey("key2")).english);
			Assert.Equal("valuee 1", ((LocBlock)locs.GetLocBlockForKey("key1")).french);
			Assert.Equal("valuee \"subquoted\" 2", ((LocBlock)locs.GetLocBlockForKey("key2")).french);
		}

		[Fact]
		public void LocalisationsReturnNullForMissingKey() {
			var locs = new LocalizationMapper();
			Assert.Null(locs.GetLocBlockForKey("key1"));
		}

		[Fact]
		public void LocalisationsReturnsEnglishForMissingLanguage() {
			var locs = new LocalizationMapper();
			var reader = new BufferedReader(
				CommonFunctions.UTF8BOM + "l_english:\n" +
				" key1: \"value 1\" # comment\n"
			 );
			locs.ScrapeStream(reader, "english");

			Assert.Equal("value 1", ((LocBlock)locs.GetLocBlockForKey("key1")).french);
		}
	}
}
