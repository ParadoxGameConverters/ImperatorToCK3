using commonItems;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Mappers.Trait;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Titles {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class LandedTitlesTests {
		[Fact]
		public void TitlesDefaultToEmpty() {
			var reader = new BufferedReader(string.Empty);
			var titles = new Title.LandedTitles();
			titles.LoadTitles(reader);

			Assert.Empty(titles);
		}

		[Fact]
		public void TitlesCanBeLoaded() {
			var reader = new BufferedReader(
				"b_barony = { province = 12 }\n" +
				"c_county = { landless = yes }\n"
			);

			var titles = new Title.LandedTitles();
			titles.LoadTitles(reader);

			var barony = titles["b_barony"];
			var county = titles["c_county"];

			Assert.Equal(2, titles.Count);
			Assert.Equal((ulong)12, barony.Province);
			Assert.True(county.Landless);
		}

		[Fact]
		public void TitlesCanBeLoadedRecursively() {
			var reader = new BufferedReader(
				"e_empire1 = { k_kingdom2 = { d_duchy3 = { b_barony4 = { province = 12 } } } }\n" +
				"c_county5 = { landless = yes }\n"
			);

			var titles = new Title.LandedTitles();
			titles.LoadTitles(reader);

			var barony = titles["b_barony4"];
			var county = titles["c_county5"];

			Assert.Equal(5, titles.Count);
			Assert.Equal((ulong)12, barony.Province);
			Assert.True(county.Landless);
		}

		[Fact]
		public void TitlesCanBeOverriddenByMods() {
			var reader = new BufferedReader(
				"e_empire1 = { k_kingdom2 = { d_duchy3 = { b_barony4 = { province = 12 } } } }\n" +
				"c_county5 = { landless = yes }\n"
			);

			var titles = new Title.LandedTitles();
			titles.LoadTitles(reader);

			var reader2 = new BufferedReader(
				"b_barony4 = { province = 15 }\n" +
				"c_county5 = { landless = no }\n"
			);
			titles.LoadTitles(reader2);

			var barony = titles["b_barony4"];
			var county = titles["c_county5"];

			Assert.Equal(5, titles.Count);
			Assert.Equal((ulong)15, barony.Province);
			Assert.False(county.Landless);
		}

		[Fact]
		public void TitlesCanBeAddedByMods() {
			var reader = new BufferedReader(
				"e_empire1 = { k_kingdom2 = { d_duchy3 = { b_barony4 = { province = 12 } } } }\n" +
				"c_county5 = { landless = yes }\n"
			);

			var titles = new Title.LandedTitles();
			titles.LoadTitles(reader);

			var reader2 = new BufferedReader(
				"c_county5 = { landless = no }\n" + // Overrides existing instance
				"e_empire6 = { k_kingdom7 = { d_duchy8 = { b_barony9 = { province = 12 } } } }\n" +
				"c_county10 = { landless = yes }\n"
			);
			titles.LoadTitles(reader2);

			Assert.Equal(10, titles.Count);
		}

		[Fact]
		public void CapitalsAreLinked() {
			var reader = new BufferedReader(
				"e_empire = {" +
				"\tcapital=c_county " +
				"\tk_kingdom = { d_duchy = { c_county = { b_barony = { province = 12 } } } } " +
				"}"
			);
			var titles = new Title.LandedTitles();
			titles.LoadTitles(reader);

			var empire = titles["e_empire"];
			var capitalCounty = empire.CapitalCounty;
			Assert.NotNull(capitalCounty);
			Assert.Equal("c_county", capitalCounty.Id);
			Assert.Equal("c_county", empire.CapitalCountyId);
		}

		[Fact]
		public void GovernorshipsCanBeRecognizedAsCountyLevel() {
			var imperatorWorld = new ImperatorToCK3.Imperator.World();

			var governor = new ImperatorToCK3.Imperator.Characters.Character(25212);
			imperatorWorld.Characters.Add(governor);

			var countryReader = new BufferedReader("tag=PRY");
			var country = ImperatorToCK3.Imperator.Countries.Country.Parse(countryReader, 589);
			imperatorWorld.Countries.Add(country);

			var areaReader = new BufferedReader("provinces = { 1 2 3 }");
			var galatiaArea = new ImperatorArea(areaReader);
			var regionReader = new BufferedReader("areas = {galatia_area}");
			var galatiaRegion = new ImperatorRegion("galatia_region", regionReader);

			var reader = new BufferedReader(
				"who=589\n" +
				"character=25212\n" +
				"start_date=450.10.1\n" +
				"governorship = \"galatia_region\""
			);
			var governorship1 = new Governorship(reader);
			imperatorWorld.Jobs.Governorships.Add(governorship1);
			var titles = new Title.LandedTitles();
			var countyLevelGovernorships = new List<Governorship>();

			var tagTitleMapper = new TagTitleMapper();
			var provinceMapper = new ProvinceMapper();
			var locMapper = new LocalizationMapper();
			var religionMapper = new ReligionMapper();
			var cultureMapper = new CultureMapper();
			var coaMapper = new CoaMapper();
			var definiteFormMapper = new DefiniteFormMapper();
			var traitMapper = new TraitMapper();
			var nicknameMapper = new NicknameMapper();
			var deathReasonMapper = new DeathReasonMapper();
			var conversionDate = new Date(500, 1, 1);

			// import Imperator governor
			var characters = new ImperatorToCK3.CK3.Characters.CharacterCollection();
			characters.ImportImperatorCharacters(imperatorWorld, religionMapper, cultureMapper, traitMapper, nicknameMapper, locMapper, provinceMapper, deathReasonMapper, conversionDate, conversionDate);

			// import country 589
			titles.ImportImperatorCountries(imperatorWorld.Countries, tagTitleMapper, locMapper, provinceMapper, coaMapper, new GovernmentMapper(), new SuccessionLawMapper(), definiteFormMapper, religionMapper, cultureMapper, nicknameMapper, characters, conversionDate);
			Assert.Collection(titles,
				title => {
					Assert.Equal("d_IMPTOCK3_PRY", title.Id);
				});

			// country 589 is imported as duchy-level title, so its governorship of galatia_region will be county level
			titles.ImportImperatorGovernorships(imperatorWorld, new ProvinceCollection(), tagTitleMapper, locMapper, provinceMapper, definiteFormMapper, new ImperatorRegionMapper(), coaMapper, countyLevelGovernorships);

			Assert.Collection(titles,
				title => {
					Assert.Equal("d_IMPTOCK3_PRY", title.Id);
				}
			// governorship is not added as a new title
			);
			Assert.Collection(countyLevelGovernorships,
				clg1 => {
					Assert.Equal("galatia_region", clg1.RegionName);
					Assert.Equal((ulong)589, clg1.CountryId);
					Assert.Equal((ulong)25212, clg1.CharacterId);
				});
		}
	}
}
