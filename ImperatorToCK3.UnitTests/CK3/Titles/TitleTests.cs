using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Titles {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class TitleTests {
		private class TitleBuilder {
			private Country country = new(0);
			private Dictionary<ulong, Country> imperatorCountries = new();
			private LocalizationMapper localizationMapper = new();
			private LandedTitles landedTitles = new();
			private ProvinceMapper provinceMapper = new();
			private CoaMapper coaMapper = new("TestFiles/CoatsOfArms.txt");
			private TagTitleMapper tagTitleMapper = new("TestFiles/configurables/title_map.txt", "TestFiles/configurables/governorMappings.txt");
			private GovernmentMapper governmentMapper = new();
			private SuccessionLawMapper successionLawMapper = new("TestFiles/configurables/succession_law_map.txt");
			private DefiniteFormMapper definiteFormMapper = new("TestFiles/configurables/definite_form_names.txt");

			private readonly ReligionMapper religionMapper = new();
			private readonly CultureMapper cultureMapper = new();
			private readonly NicknameMapper nicknameMapper = new("TestFiles/configurables/nickname_map.txt");
			private readonly Dictionary<string, Character> charactersDict = new();
			private readonly Date ck3BookmarkDate = new Date(867, 1, 1);

			public Title BuildFromTag() {
				return new Title(
					country,
					imperatorCountries,
					localizationMapper,
					landedTitles,
					provinceMapper,
					coaMapper,
					tagTitleMapper,
					governmentMapper,
					successionLawMapper,
					definiteFormMapper,
					religionMapper,
					cultureMapper,
					nicknameMapper,
					charactersDict,
					ck3BookmarkDate
				);
			}
			public TitleBuilder WithCountry(Country country) {
				this.country = country;
				return this;
			}
			public TitleBuilder WithImperatorCountries(Dictionary<ulong, Country> imperatorCountries) {
				this.imperatorCountries = imperatorCountries;
				return this;
			}
			public TitleBuilder WithLocalizationMapper(LocalizationMapper localizationMapper) {
				this.localizationMapper = localizationMapper;
				return this;
			}
			public TitleBuilder WithLandedTitles(LandedTitles landedTitles) {
				this.landedTitles = landedTitles;
				return this;
			}
			public TitleBuilder WithProvinceMapper(ProvinceMapper provinceMapper) {
				this.provinceMapper = provinceMapper;
				return this;
			}
			public TitleBuilder WithCoatsOfArmsMapper(CoaMapper coaMapper) {
				this.coaMapper = coaMapper;
				return this;
			}
			public TitleBuilder WithTagTitleMapper(TagTitleMapper tagTitleMapper) {
				this.tagTitleMapper = tagTitleMapper;
				return this;
			}
			public TitleBuilder WithGovernmentMapper(GovernmentMapper governmentMapper) {
				this.governmentMapper = governmentMapper;
				return this;
			}
			public TitleBuilder WithSuccessionLawMapper(SuccessionLawMapper successionLawMapper) {
				this.successionLawMapper = successionLawMapper;
				return this;
			}
			public TitleBuilder WithDefiniteFormMapper(DefiniteFormMapper definiteFormMapper) {
				this.definiteFormMapper = definiteFormMapper;
				return this;
			}
		}

		private readonly TitleBuilder builder = new();

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
				"province = 345\n" +
				"c_roma = {}"
			);

			var title = new Title("k_testtitle");
			title.LoadTitles(reader);

			Assert.True(title.HasDefiniteForm);
			Assert.True(title.Landless);
			Assert.NotNull(title.Color1);
			Assert.Equal("rgb { 23 23 23 }", title.Color1.OutputRgb());
			Assert.Null(title.CapitalCounty); // capital county not linked yet
			Assert.Equal((ulong)345, title.Province);

			var roma = new Title("c_roma");
			var titles = new LandedTitles();
			titles.InsertTitle(roma);
			titles.InsertTitle(title);
			Assert.NotNull(title.CapitalCounty);
			Assert.Equal("c_roma", title.CapitalCountyName);
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
			var date = new Date(867, 1, 1);
			var titlesHistory = new TitlesHistory("TestFiles/title_history", date);
			var history = titlesHistory.PopTitleHistory("k_greece");
			Assert.NotNull(history);
			var title = new Title("k_testtitle");
			title.AddHistory(history);

			Assert.Equal("420", title.GetHolderId(date));
			Assert.Equal(20, title.GetDevelopmentLevel(date));
		}

		[Fact]
		public void DevelopmentLevelCanBeInherited() {
			var date = new Date(867, 1, 1);
			var vassal = new Title("c_vassal");
			vassal.DeJureLiege = new Title("d_liege");
			vassal.DeJureLiege.SetDevelopmentLevel(8, date);

			Assert.Equal(8, vassal.GetOwnOrInheritedDevelopmentLevel(date));
		}

		[Fact]
		public void InheritedDevelopmentCanBeNull() {
			var date = new Date(867, 1, 1);
			var vassal = new Title("c_vassal") {
				DeJureLiege = new Title("d_liege")
			};

			Assert.Null(vassal.GetOwnOrInheritedDevelopmentLevel(date));
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
			var date = new Date(476, 1, 1);
			var landedTitles = new LandedTitles();

			var empire = new Title("e_empire");
			landedTitles.InsertTitle(empire);

			var kingdom1 = new Title("k_kingdom1");
			landedTitles.InsertTitle(kingdom1);
			kingdom1.SetDeFactoLiege(empire, date);

			var kingdom2 = new Title("k_kingdom2");
			landedTitles.InsertTitle(kingdom2);
			kingdom2.SetDeFactoLiege(empire, date);

			var duchy = new Title("d_duchy");
			landedTitles.InsertTitle(duchy);
			duchy.SetDeFactoLiege(kingdom2, date);

			var county = new Title("c_county");
			landedTitles.InsertTitle(county);
			county.SetDeFactoLiege(duchy, date);

			var vassals = empire.GetDeFactoVassalsAndBelow(date, landedTitles);
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
			var date = new Date(476, 1, 1);
			var landedTitles = new LandedTitles();

			var empire = new Title("e_empire");
			landedTitles.InsertTitle(empire);

			var kingdom1 = new Title("k_kingdom1");
			landedTitles.InsertTitle(kingdom1);
			kingdom1.SetDeFactoLiege(empire, date);

			var kingdom2 = new Title("k_kingdom2");
			landedTitles.InsertTitle(kingdom2);
			kingdom2.SetDeFactoLiege(empire, date);

			var duchy = new Title("d_duchy");
			landedTitles.InsertTitle(duchy);
			duchy.SetDeFactoLiege(kingdom2, date);

			var county = new Title("c_county");
			landedTitles.InsertTitle(county);
			county.SetDeFactoLiege(duchy, date);

			var vassals = empire.GetDeFactoVassalsAndBelow(date, landedTitles, rankFilter: "ck");
			var sortedVassals = from entry in vassals orderby entry.Key ascending select entry;
			Assert.Collection(sortedVassals,
				// only counties and kingdoms go through the filter
				item1 => Assert.Equal("c_county", item1.Value.Name),
				item2 => Assert.Equal("k_kingdom1", item2.Value.Name),
				item3 => Assert.Equal("k_kingdom2", item3.Value.Name)
			);
		}

		[Fact]
		public void DeFactoLiegeChangeRemovesTitleFromVassalsOfPreviousLiege() {
			var date = new Date(476, 1, 1);
			var landedTitles = new LandedTitles();

			var vassal = new Title("d_vassal");
			landedTitles.InsertTitle(vassal);
			var oldLiege = new Title("k_old_liege");
			landedTitles.InsertTitle(oldLiege);
			vassal.SetDeFactoLiege(oldLiege, date);
			Assert.Equal("k_old_liege", vassal.GetDeFactoLiege(date,landedTitles).Name);
			Assert.True(oldLiege.GetDeFactoVassals(date, landedTitles).ContainsKey("d_vassal"));

			var newLiege = new Title("k_new_liege");
			landedTitles.InsertTitle(newLiege);
			vassal.SetDeFactoLiege(newLiege, date);
			Assert.Equal("k_new_liege", vassal.GetDeFactoLiege(date, landedTitles).Name);
			Assert.False(oldLiege.GetDeFactoVassals(date, landedTitles).ContainsKey("d_vassal"));
			Assert.True(newLiege.GetDeFactoVassals(date,landedTitles).ContainsKey("d_vassal"));
		}

		[Fact]
		public void DeJureLiegeChangeRemovesTitleFromVassalsOfPreviousLiege() {
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
		[Fact]
		public void KingdomContainsProvinceCorrectlyReturnsFalse() {
			var county = new Title("c_county");
			county.CountyProvinces.Add(1);
			var duchy = new Title("d_duchy");
			county.DeJureLiege = duchy;
			var kingdom = new Title("k_kingdom");
			duchy.DeJureLiege = kingdom;
			Assert.False(kingdom.KingdomContainsProvince(2));
		}

		[Fact]
		public void TitleCanBeConstructedFromCountry() {
			var countryReader = new BufferedReader("tag = HRE");
			var country = Country.Parse(countryReader, 666);

			var title = builder
				.WithCountry(country)
				.BuildFromTag();
			Assert.Equal("d_IMPTOCK3_HRE", title.Name);
		}
	}
}
