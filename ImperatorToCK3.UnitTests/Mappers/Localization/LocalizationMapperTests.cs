using Xunit;
using ImperatorToCK3.Mappers.Localizaton;
using commonItems;

namespace ImperatorToCK3.UnitTests.Mappers.Localization {
    public class LocalizationMapperTests {
		[Fact] public void LocalisationsCanBeLoadedAndMatched() {
			var reader1 = new BufferedReader(
				CommonFunctions.UTF8BOM + "l_english:\n" +
				" key1:0 \"value 1\" # comment\n" +
				" key2:0 \"value \"subquoted\" 2\"\n"
			);
			var reader2 = new BufferedReader(
				CommonFunctions.UTF8BOM + "l_french:\n" +
				" key1:0 \"valuee 1\"\n" +
				" key2:0 \"valuee \"subquoted\" 2\"\n"
			);
			var reader3 = new BufferedReader(
				CommonFunctions.UTF8BOM + "l_english:\n" +
				" key1:0 \"replaced value 1\"\n"
			);

			var locs = new LocalizationMapper();
			locs.ScrapeStream(reader1, "english");
			locs.ScrapeStream(reader2, "french");
			locs.ScrapeStream(reader3, "english");

			Assert.Equal("replaced value 1", locs.GetLocBlockForKey("key1").english);
			Assert.Equal("value \"subquoted\" 2", locs.GetLocBlockForKey("key2").english);
			Assert.Equal("valuee 1", locs.GetLocBlockForKey("key1").french);
			Assert.Equal("valuee \"subquoted\" 2", locs.GetLocBlockForKey("key2").french);
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
				" key1:1 \"value 1\" # comment\n"
			 );
			locs.ScrapeStream(reader, "english");

			Assert.Equal("value 1", ((LocBlock)locs.GetLocBlockForKey("key1")).french);
		}

		[Fact] public void LocCanBeModifiedByMethodForEveryLanguage() {
			var nameLocBlock = new LocBlock {
				english = "$ADJ$ Revolt",
				french = "$ADJ$ révolte",
				german = "$ADJ$ Revolte",
				russian = "$ADJ$ бунт",
				simp_chinese = "$ADJ$ 反叛",
				spanish = "$ADJ$ revuelta"
			};
			var adjLocBlock = new LocBlock {
				english = "Roman",
				french = "Romain",
				german = "römisch",
				russian = "Роман",
				simp_chinese = "罗马",
				spanish = "Romana"
			};
			nameLocBlock.ModifyForEveryLanguage(adjLocBlock, (ref string baseLoc, string modifyingLoc) => {
				baseLoc = baseLoc.Replace("$ADJ$", modifyingLoc);
			});
			Assert.Equal("Roman Revolt", nameLocBlock.english);
			Assert.Equal("Romain révolte", nameLocBlock.french);
			Assert.Equal("römisch Revolte", nameLocBlock.german);
			Assert.Equal("Роман бунт", nameLocBlock.russian);
			Assert.Equal("罗马 反叛", nameLocBlock.simp_chinese);
			Assert.Equal("Romana revuelta", nameLocBlock.spanish);
		}

		[Fact] public void LocalizationCanBeAddedForKey() {
			var localizationMapper = new LocalizationMapper();
			Assert.Null(localizationMapper.GetLocBlockForKey("key1"));
			localizationMapper.AddLocalization("key1", new LocBlock {
				english = "Roman", french = "Romain"
			});

			var locBlock = localizationMapper.GetLocBlockForKey("key1");
			Assert.Equal("Roman", locBlock.english);
			Assert.Equal("Romain", locBlock.french);
			Assert.Equal("Roman", locBlock.german);
			Assert.Equal("Roman", locBlock.russian);
			Assert.Equal("Roman", locBlock.simp_chinese);
			Assert.Equal("Roman", locBlock.spanish);
        }
	}
}
