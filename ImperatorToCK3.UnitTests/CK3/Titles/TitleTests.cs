using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Localization;
using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Titles {
	public class TitleTests {
		[Fact]
		public void TitlePrimitivesDefaultToBlank() {
			var reader = new BufferedReader(string.Empty);
			var title = new Title();
			title.LoadTitles(reader);

			Assert.False(title.HasDefiniteForm);
			Assert.False(title.Landless);
			Assert.Null(title.Color);
			Assert.Null(title.CapitalCounty);
			Assert.Null(title.Province);
		}

		[Fact]
		public void TitlePrimitivesCanBeLoaded() {
			var reader = new BufferedReader(
				"definite_form = yes\n" +
				"landless = yes\n" +
				"color = { 23 23 23 }\n" +
				"capital = c_roma\n" +
				"province = 345\n"
			);

			var title = new Title();
			title.LoadTitles(reader);

			Assert.True(title.HasDefiniteForm);
			Assert.True(title.Landless);
			Assert.Equal("= rgb { 23 23 23 }", title.Color.OutputRgb());
			Assert.Equal("c_roma", title.CapitalCounty.Value.Key);
			Assert.Equal((ulong)345, title.Province);
		}

		[Fact]
		public void LocalizationCanBeSet() {
			var title = new Title();
			var locBlock = new LocBlock {
				english = "engloc",
				french = "frloc",
				german = "germloc",
				russian = "rusloc",
				spanish = "spaloc"
			};

			title.SetNameLoc(locBlock);
			Assert.Equal(1, title.Localizations.Count);
		}

		[Fact]
		public void MembersDefaultToBlank() {
			var title = new Title();

			Assert.True(string.IsNullOrEmpty(title.Name));
			Assert.Empty(title.Localizations);
			Assert.Null(title.CoA);
			Assert.Null(title.CapitalCounty);
		}

		[Fact]
		public void HolderIdDefaultsTo0String() {
			var title = new Title();

			Assert.Equal("0", title.HolderID);
		}

		[Fact]
		public void HolderPtrDefaultsToNull() {
			var title = new Title();

			Assert.Null(title.Holder);
		}

		[Fact]
		public void CapitalBaronyDefaultsToZero() {
			var title = new Title();

			Assert.Equal((ulong)0, title.CapitalBaronyProvince);
		}

		[Fact]
		public void HistoryCanBeAdded() {
			var titlesHistory = new TitlesHistory("TestFiles/title_history");
			var history = titlesHistory.PopTitleHistory("k_greece");
			var title = new Title();
			title.AddHistory(new LandedTitles(), history);

			Assert.Equal("420", title.HolderID);
			Assert.Equal(20, title.DevelopmentLevel);
		}

		[Fact]
		public void DevelopmentLevelCanBeInherited() {
			var vassal = new Title("c_vassal");
			vassal.DeJureLiege = new Title("d_liege") {
				DevelopmentLevel = 8
			};

			Assert.Equal(8, vassal.OwnOrInheritedDevelopmentLevel);
		}

		[Fact]
		public void InheritedDevelopmentCanBeNullopt() {
			var vassal = new Title("c_vassal");
			vassal.DeJureLiege = new Title("d_liege");

			Assert.Null(vassal.OwnOrInheritedDevelopmentLevel);
		}
	}
}
