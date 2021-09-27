using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Localization;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Titles {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class TitleTests {
		[Fact]
		public void TitlePrimitivesDefaultToBlank() {
			var reader = new BufferedReader(string.Empty);
			var title = new Title("k_testtitle");
			title.LoadTitles(reader);

			Assert.False(title.HasDefiniteForm);
			Assert.False(title.Landless);
			Assert.Null(title.Color1);
			Assert.Null(title.Color2);
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

			var title = new Title("k_testtitle");
			title.LoadTitles(reader);

			Assert.True(title.HasDefiniteForm);
			Assert.True(title.Landless);
			Assert.Equal("= rgb { 23 23 23 }", title.Color1.OutputRgb());
			Assert.Equal("c_roma", title.CapitalCounty.Value.Key);
			Assert.Equal((ulong)345, title.Province);
		}

		[Fact]
		public void LocalizationCanBeSet() {
			var title = new Title("k_testtitle");
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
			var title = new Title("k_testtitle");

			Assert.Empty(title.Localizations);
			Assert.Null(title.CoA);
			Assert.Null(title.CapitalCounty);
			Assert.Null(title.ImperatorCountry);
		}

		[Fact]
		public void HolderIdDefaultsTo0String() {
			var title = new Title("k_testtitle");

			Assert.Equal("0", title.GetHolderId(new Date(867, 1, 1)));
		}

		[Fact]
		public void CapitalBaronyDefaultsToZero() {
			var title = new Title("k_testtitle");

			Assert.Equal((ulong)0, title.CapitalBaronyProvince);
		}

		[Fact]
		public void HistoryCanBeAdded() {
			var titlesHistory = new TitlesHistory("TestFiles/title_history", new Date(867, 1, 1));
			var history = titlesHistory.PopTitleHistory("k_greece");
			var title = new Title("k_testtitle");
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

		[Fact]
		public void DeJureVassalsAndBelowAreCorrectlyReturned() {
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

		[Fact] public void DeFactoLiegeChangeRemovesTitleFromVassalsOfPreviousLege() {
			var vassal = new Title("d_vassal");
			var oldLiege = new Title("k_old_liege");
			vassal.DeFactoLiege = oldLiege;
			Assert.True(oldLiege.DeFactoVassals.ContainsKey("d_vassal"));

			var newLiege = new Title("k_new_liege");
			vassal.DeFactoLiege = newLiege;
			Assert.False(oldLiege.DeFactoVassals.ContainsKey("d_vassal"));
			Assert.True(newLiege.DeFactoVassals.ContainsKey("d_vassal"));
		}

		[Fact]
		public void DeJureLiegeChangeRemovesTitleFromVassalsOfPreviousLege() {
			var vassal = new Title("d_vassal");
			var oldLiege = new Title("k_old_liege");
			vassal.DeJureLiege = oldLiege;
			Assert.Equal("k_old_liege", vassal.DeJureLiege.Name);
			Assert.True(oldLiege.DeJureVassals.ContainsKey("d_vassal"));

			var newLiege = new Title("k_new_liege");
			vassal.DeJureLiege = newLiege;
			Assert.Equal("k_new_liege", vassal.DeJureLiege.Name);
			Assert.False(oldLiege.DeJureVassals.ContainsKey("d_vassal"));
			Assert.True(newLiege.DeJureVassals.ContainsKey("d_vassal"));
		}

		[Fact]
		public void DuchyContainsProvinceWhenTitleIsNotDuchy() {
			var county = new Title("c_county");
			county.CountyProvinces.Add(69);
			Assert.False(county.DuchyContainsProvince(69));
		}
		[Fact]
		public void DuchyContainsProvinceCorrectlyReturnsTrue() {
			var county = new Title("c_county");
			county.CountyProvinces.Add(1);
			var duchy = new Title("d_duchy");
			county.DeJureLiege = duchy;
			Assert.True(duchy.DuchyContainsProvince(1));
		}
		[Fact]
		public void DuchyContainsProvinceCorrectlyReturnsFalse() {
			var county = new Title("c_county");
			county.CountyProvinces.Add(1);
			var duchy = new Title("d_duchy");
			county.DeJureLiege = duchy;
			Assert.False(duchy.DuchyContainsProvince(2));
		}

		[Fact]
		public void KingdomContainsProvinceWhenTitleIsNotKingdom() {
			var county = new Title("c_county");
			county.CountyProvinces.Add(69);
			Assert.False(county.KingdomContainsProvince(69));
		}
		[Fact]
		public void KingdomContainsProvinceCorrectlyReturnsTrue() {
			var county = new Title("c_county");
			county.CountyProvinces.Add(1);
			var duchy = new Title("d_duchy");
			county.DeJureLiege = duchy;
			var kingdom = new Title("k_kingdom");
			duchy.DeJureLiege = kingdom;
			Assert.True(kingdom.KingdomContainsProvince(1));
		}
		[Fact] public void KingdomContainsProvinceCorrectlyReturnsFalse() {
			var county = new Title("c_county");
			county.CountyProvinces.Add(1);
			var duchy = new Title("d_duchy");
			county.DeJureLiege = duchy;
			var kingdom = new Title("k_kingdom");
			duchy.DeJureLiege = kingdom;
			Assert.False(kingdom.KingdomContainsProvince(2));
		}
	}
}
