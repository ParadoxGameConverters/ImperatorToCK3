﻿using commonItems;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.DeathReason;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using ImperatorToCK3.Mappers.Trait;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using ProvinceCollection = ImperatorToCK3.CK3.Provinces.ProvinceCollection;

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
			var config = new Configuration { ImperatorPath = "TestFiles/LandedTitlesTests/Imperator" };
			var imperatorWorld = new ImperatorToCK3.Imperator.World(config);

			imperatorWorld.Provinces.Add(new Province(1));
			imperatorWorld.Provinces.Add(new Province(2));
			imperatorWorld.Provinces.Add(new Province(3));

			var governor = new ImperatorToCK3.Imperator.Characters.Character(25212);
			imperatorWorld.Characters.Add(governor);

			var countryReader = new BufferedReader("tag=PRY capital=1");
			var country = ImperatorToCK3.Imperator.Countries.Country.Parse(countryReader, 589);
			imperatorWorld.Countries.Add(country);

			var impRegionMapper = new ImperatorRegionMapper(imperatorWorld.ModFS);
			Assert.True(impRegionMapper.RegionNameIsValid("galatia_area"));
			Assert.True(impRegionMapper.RegionNameIsValid("galatia_region"));
			var ck3RegionMapper = new CK3RegionMapper();

			var reader = new BufferedReader(
				"who=589 " +
				"character=25212 " +
				"start_date=450.10.1 " +
				"governorship = \"galatia_region\""
			);
			var governorship1 = new Governorship(reader);
			imperatorWorld.Jobs.Governorships.Add(governorship1);
			var titles = new Title.LandedTitles();
			titles.LoadTitles(new BufferedReader(
				"c_county1 = { b_barony1={province=1} } " +
				"c_county2 = { b_barony2={province=2} } " +
				"c_county3 = { b_barony3={province=3} }")
			);
			var countyLevelGovernorships = new List<Governorship>();

			var tagTitleMapper = new TagTitleMapper();
			var provinceMapper = new ProvinceMapper(
				new BufferedReader("0.0.0.0 = {" +
								   "\tlink={imp=1 ck3=1}" +
								   "\tlink={imp=2 ck3=2}" +
								   "\tlink={imp=3 ck3=3}" +
								   "}"
				)
			);
			var locDB = new LocDB("english");
			var religionMapper = new ReligionMapper(impRegionMapper, ck3RegionMapper);
			var cultureMapper = new CultureMapper(impRegionMapper, ck3RegionMapper);
			var coaMapper = new CoaMapper();
			var definiteFormMapper = new DefiniteFormMapper();
			var traitMapper = new TraitMapper();
			var nicknameMapper = new NicknameMapper();
			var deathReasonMapper = new DeathReasonMapper();
			var conversionDate = new Date(500, 1, 1);

			// Import Imperator governor.
			var characters = new ImperatorToCK3.CK3.Characters.CharacterCollection();
			characters.ImportImperatorCharacters(imperatorWorld, religionMapper, cultureMapper, traitMapper, nicknameMapper, locDB, provinceMapper, deathReasonMapper, conversionDate, config);

			// Import country 589.
			titles.ImportImperatorCountries(imperatorWorld.Countries, tagTitleMapper, locDB, provinceMapper, coaMapper, new GovernmentMapper(), new SuccessionLawMapper(), definiteFormMapper, religionMapper, cultureMapper, nicknameMapper, characters, conversionDate, config);
			Assert.Collection(titles,
				title => Assert.Equal("c_county1", title.Id),
				title => Assert.Equal("b_barony1", title.Id),
				title => Assert.Equal("c_county2", title.Id),
				title => Assert.Equal("b_barony2", title.Id),
				title => Assert.Equal("c_county3", title.Id),
				title => Assert.Equal("b_barony3", title.Id),
				title => Assert.Equal("d_IMPTOCK3_PRY", title.Id)
			);

			var provinces = new ProvinceCollection("TestFiles/LandedTitlesTests/CK3/provinces.txt", conversionDate);
			provinces.ImportImperatorProvinces(imperatorWorld, titles, cultureMapper, religionMapper, provinceMapper, config);
			// Country 589 is imported as duchy-level title, so its governorship of galatia_region will be county level.
			titles.ImportImperatorGovernorships(imperatorWorld, provinces, tagTitleMapper, locDB, provinceMapper, definiteFormMapper, impRegionMapper, coaMapper, countyLevelGovernorships);

			Assert.Collection(titles,
				title => Assert.Equal("c_county1", title.Id),
				title => Assert.Equal("b_barony1", title.Id),
				title => Assert.Equal("c_county2", title.Id),
				title => Assert.Equal("b_barony2", title.Id),
				title => Assert.Equal("c_county3", title.Id),
				title => Assert.Equal("b_barony3", title.Id),
				title => Assert.Equal("d_IMPTOCK3_PRY", title.Id)
			// Governorship is not added as a new title.
			);
			Assert.Collection(countyLevelGovernorships,
				clg1 => {
					Assert.Equal("galatia_region", clg1.RegionName);
					Assert.Equal((ulong)589, clg1.CountryId);
					Assert.Equal((ulong)25212, clg1.CharacterId);
				}
			);
		}

		[Fact]
		public void DevelopmentIsNotChangedForCountiesOutsideOfImperatorMap() {
			var date = new Date(476, 1, 1);
			var titles = new Title.LandedTitles();
			var county = titles.Add("c_county");
			county.SetDevelopmentLevel(33, date);

			var imperatorProvinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection();
			var provMapper = new ProvinceMapper();

			titles.ImportDevelopmentFromImperator(imperatorProvinces, provMapper, date);

			Assert.Equal(33, county.GetDevelopmentLevel(date));
		}

		[Fact]
		public void DevelopmentIsCorrectlyCalculatedFor1ProvinceTo1BaronyCountyMapping() {
			var date = new Date(476, 1, 1);
			var titles = new Title.LandedTitles();
			var titlesReader = new BufferedReader(
				"c_county1={ b_barony1={province=1} } "
			);
			titles.LoadTitles(titlesReader);

			var imperatorProvinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection();
			var impProv = new ImperatorToCK3.Imperator.Provinces.Province(1) { CivilizationValue = 25 };
			imperatorProvinces.Add(impProv);

			var mappingsReader = new BufferedReader("0.0.0.0={ link={ imp=1 ck3=1 } }");
			var provMapper = new ProvinceMapper(mappingsReader);

			titles.ImportDevelopmentFromImperator(imperatorProvinces, provMapper, date);

			Assert.Equal(20, titles["c_county1"].GetDevelopmentLevel(date)); // 25 - sqrt(25)
		}

		[Fact]
		public void DevelopmentFromImperatorProvinceCanBeSplitForTargetProvinces() {
			var date = new Date(476, 1, 1);
			var titles = new Title.LandedTitles();
			var titlesReader = new BufferedReader(
				"c_county1={ b_barony1={province=1} } " +
				"c_county2={ b_barony2={province=2} } " +
				"c_county3={ b_barony3={province=3} } "
			);
			titles.LoadTitles(titlesReader);

			var imperatorProvinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection();
			var impProv = new ImperatorToCK3.Imperator.Provinces.Province(1) { CivilizationValue = 21 };
			imperatorProvinces.Add(impProv);

			var mappingsReader = new BufferedReader("0.0.0.0={ link={ imp=1 ck3=1 ck3=2 ck3=3 } }");
			var provMapper = new ProvinceMapper(mappingsReader);

			titles.ImportDevelopmentFromImperator(imperatorProvinces, provMapper, date);

			Assert.Equal(4, titles["c_county1"].GetDevelopmentLevel(date)); // 7 - sqrt(7)
			Assert.Equal(4, titles["c_county2"].GetDevelopmentLevel(date)); // same
			Assert.Equal(4, titles["c_county3"].GetDevelopmentLevel(date)); // same
		}

		[Fact]
		public void DevelopmentOfCountyIsCalculatedFromAllCountyProvinces() {
			var date = new Date(476, 1, 1);
			var titles = new Title.LandedTitles();
			var titlesReader = new BufferedReader(
				"c_county1={ b_barony1={province=1} b_barony2={province=2} } "
			);
			titles.LoadTitles(titlesReader);

			var imperatorProvinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection();
			var impProv1 = new ImperatorToCK3.Imperator.Provinces.Province(1) { CivilizationValue = 10 };
			imperatorProvinces.Add(impProv1);
			var impProv2 = new ImperatorToCK3.Imperator.Provinces.Province(2) { CivilizationValue = 40 };
			imperatorProvinces.Add(impProv2);

			var mappingsReader = new BufferedReader("0.0.0.0={" +
													" link={ imp=1 ck3=1 } " +
													" link={ imp=2 ck3=2 } " +
													"}");
			var provMapper = new ProvinceMapper(mappingsReader);

			titles.ImportDevelopmentFromImperator(imperatorProvinces, provMapper, date);

			Assert.Equal(20, titles["c_county1"].GetDevelopmentLevel(date)); // (10+40)/2 - sqrt(25)
		}

		[Fact]
		public void DerivedColorsHaveCorrectComponents() {
			var titles = new Title.LandedTitles();
			var baseColor = new Color(0.2, 0.3, 0.4);

			var baseTitle = titles.Add("e_base");
			baseTitle.Color1 = baseColor;

			var derivedTitle1 = titles.Add("k_derived1");
			var derivedColor1 = titles.GetDerivedColor(baseColor);
			derivedTitle1.Color1 = derivedColor1;

			var derivedTitle2 = titles.Add("k_derived2");
			var derivedColor2 = titles.GetDerivedColor(baseColor);
			derivedTitle2.Color1 = derivedColor2;

			Assert.Equal(baseColor.H, derivedColor1.H);
			Assert.Equal(baseColor.S, derivedColor1.S);
			Assert.NotEqual(baseColor.V, derivedColor1.V);

			Assert.Equal(baseColor.H, derivedColor2.H);
			Assert.Equal(baseColor.S, derivedColor2.S);
			Assert.NotEqual(baseColor.V, derivedColor2.V);

			Assert.NotEqual(derivedColor1.V, derivedColor2.V);
		}

		[Fact]
		public void WarningIsLoggedWhenColorCanNotBeDerived() {
			var titles = new Title.LandedTitles();
			var baseColor = new Color(0.2, 0.3, 0.4);
			var baseTitle = titles.Add("e_base");
			baseTitle.Color1 = baseColor;

			for (double v = 0; v <= 1; v += 0.01) {
				var color = new Color(baseColor.H, baseColor.S, v);
				var title = titles.Add($"k_{color.OutputHex()}");
				title.Color1 = color;
			}

			var logWriter = new StringWriter();
			Console.SetOut(logWriter);
			_ = titles.GetDerivedColor(baseColor);
			Assert.Contains("Couldn't generate new color from base", logWriter.ToString());
		}

		[Fact]
		public void HistoryCanBeLoadedFromInitialValues() {
			var date = new Date(867, 1, 1);
			var config = new Configuration {
				CK3BookmarkDate = date,
				CK3Path = "TestFiles/LandedTitlesTests/CK3"
			};
			var ck3ModFS = new ModFilesystem(Path.Combine(config.CK3Path, "game"), new List<Mod>());

			var titles = new Title.LandedTitles();
			var title = titles.Add("k_rome");

			titles.LoadHistory(config, ck3ModFS);

			Assert.Equal("67", title.GetHolderId(date));
			Assert.Equal("e_italia", title.GetLiege(date));
		}

		[Fact]
		public void HistoryIsLoadedFromDatedBlocks() {
			var date = new Date(867, 1, 1);
			var config = new Configuration {
				CK3BookmarkDate = date,
				CK3Path = "TestFiles/LandedTitlesTests/CK3"
			};
			var ck3ModFS = new ModFilesystem(Path.Combine(config.CK3Path, "game"), new List<Mod>());

			var titles = new Title.LandedTitles();
			var title = titles.Add("k_greece");

			titles.LoadHistory(config, ck3ModFS);

			Assert.Equal("420", title.GetHolderId(date));
			Assert.Equal(20, title.GetDevelopmentLevel(date));
		}
	}
}
