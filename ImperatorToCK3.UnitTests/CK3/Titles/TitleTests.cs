using System.Linq;
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
			Assert.False(title.PlayerCountry);
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

			Assert.Equal("0", title.GetHolderId(new Date(867,1,1)));
		}

		[Fact]
		public void CapitalBaronyDefaultsToZero() {
			var title = new Title();

			Assert.Equal((ulong)0, title.CapitalBaronyProvince);
		}

		[Fact]
		public void HistoryCanBeAdded() {
			var titlesHistory = new TitlesHistory("TestFiles/title_history", new Date(867,1,1));
			var history = titlesHistory.PopTitleHistory("k_greece");
			var title = new Title();
			title.AddHistory(new LandedTitles(), history);

			Assert.Equal("420", title.GetHolderId(new Date(867, 1, 1)));
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
			var vassal = new Title("c_vassal") {
				DeJureLiege = new Title("d_liege")
			};

			Assert.Null(vassal.OwnOrInheritedDevelopmentLevel);
		}

		[Fact] public void DeJureVassalsAndBelowAreCorrectlyReturned() {
			var empire = new Title("e_empire");

			var kingdom1 = new Title("k_kingdom1") {
				DeJureLiege = empire
			};

			var kingdom2 = new Title("k_kingdom2") {
				DeJureLiege = empire
			};
			var duchy = new Title("d_duchy") {
				DeJureLiege = kingdom2
			};
			var county = new Title("c_county") {
				DeJureLiege = duchy
			};

			var vassals = empire.GetDeJureVassalsAndBelow();
			var sortedVassals = from entry in vassals orderby entry.Key ascending select entry;
			Assert.Collection(sortedVassals,
				item1 => Assert.Equal("c_county", item1.Value.Name),
				item2 => Assert.Equal("d_duchy", item2.Value.Name),
				item3 => Assert.Equal("k_kingdom1", item3.Value.Name),
				item4 => Assert.Equal("k_kingdom2", item4.Value.Name)
			);
		}
		[Fact]
		public void DeJureVassalsAndBelowCanBeFilteredByRank() {
			var empire = new Title("e_empire");

			var kingdom1 = new Title("k_kingdom1") {
				DeJureLiege = empire
			};

			var kingdom2 = new Title("k_kingdom2") {
				DeJureLiege = empire
			};
			var duchy = new Title("d_duchy") {
				DeJureLiege = kingdom2
			};
			var county = new Title("c_county") {
				DeJureLiege = duchy
			};

			var vassals = empire.GetDeJureVassalsAndBelow(rankFilter: "ck");
			var sortedVassals = from entry in vassals orderby entry.Key ascending select entry;
			Assert.Collection(sortedVassals,
				// only counties and kingdoms go through the filter
				item1 => Assert.Equal("c_county", item1.Value.Name),
				item2 => Assert.Equal("k_kingdom1", item2.Value.Name),
				item3 => Assert.Equal("k_kingdom2", item3.Value.Name)
			);
		}

		[Fact]
		public void DeFactoVassalsAndBelowAreCorrectlyReturned() {
			var empire = new Title("e_empire");

			var kingdom1 = new Title("k_kingdom1") {
				DeFactoLiege = empire
			};

			var kingdom2 = new Title("k_kingdom2") {
				DeFactoLiege = empire
			};
			var duchy = new Title("d_duchy") {
				DeFactoLiege = kingdom2
			};
			var county = new Title("c_county") {
				DeFactoLiege = duchy
			};

			var vassals = empire.GetDeFactoVassalsAndBelow();
			var sortedVassals = from entry in vassals orderby entry.Key ascending select entry;
			Assert.Collection(sortedVassals,
				item1 => Assert.Equal("c_county", item1.Value.Name),
				item2 => Assert.Equal("d_duchy", item2.Value.Name),
				item3 => Assert.Equal("k_kingdom1", item3.Value.Name),
				item4 => Assert.Equal("k_kingdom2", item4.Value.Name)
			);
		}
		[Fact]
		public void DeFactoVassalsAndBelowCanBeFilteredByRank() {
			var empire = new Title("e_empire");

			var kingdom1 = new Title("k_kingdom1") {
				DeFactoLiege = empire
			};

			var kingdom2 = new Title("k_kingdom2") {
				DeFactoLiege = empire
			};
			var duchy = new Title("d_duchy") {
				DeFactoLiege = kingdom2
			};
			var county = new Title("c_county") {
				DeFactoLiege = duchy
			};

			var vassals = empire.GetDeFactoVassalsAndBelow(rankFilter: "ck");
			var sortedVassals = from entry in vassals orderby entry.Key ascending select entry;
			Assert.Collection(sortedVassals,
				// only counties and kingdoms go through the filter
				item1 => Assert.Equal("c_county", item1.Value.Name),
				item2 => Assert.Equal("k_kingdom1", item2.Value.Name),
				item3 => Assert.Equal("k_kingdom2", item3.Value.Name)
			);
		}
	}
}
