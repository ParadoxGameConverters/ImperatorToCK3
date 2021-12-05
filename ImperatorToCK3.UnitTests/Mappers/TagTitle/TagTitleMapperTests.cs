using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Jobs;
using ImperatorToCK3.Mappers.CoA;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Mappers.Religion;
using ImperatorToCK3.Mappers.SuccessionLaw;
using ImperatorToCK3.Mappers.TagTitle;
using System;
using System.IO;
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
			var match = mapper.GetTitleForTag("CRT", CountryRank.majorPower);

			Assert.Equal("k_krete", match);
		}
		[Fact]
		public void TitleCanBeMatchedFromGovernorship() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath); // reads title_map.txt from TestFiles
			mapper.RegisterTag("ROM", "e_roman_empire");

			var impCountries = new CountryCollection(new BufferedReader(" 1={tag=ROM}"));
			var titles = new Title.LandedTitles();
			titles.ImportImperatorCountries(impCountries,
				mapper,
				new LocalizationMapper(),
				new ProvinceMapper(),
				new CoaMapper(),
				new GovernmentMapper(),
				new SuccessionLawMapper(),
				new DefiniteFormMapper(),
				new ReligionMapper(),
				new CultureMapper(),
				new NicknameMapper(),
				new CharacterCollection(),
				new Date()
			);

			var centralItalyGov = new Governorship(new BufferedReader("who=1 governorship=central_italy_region"));
			var provinces = new ProvinceCollection();
			var imperatorRegionMapper = new ImperatorRegionMapper();
			var match = mapper.GetTitleForGovernorship(centralItalyGov, impCountries[1], titles, provinces, imperatorRegionMapper);

			Assert.Equal("k_romagna", match);
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
			mapper.RegisterTag("ROM", "e_roman_empire");
			mapper.RegisterTag("DRE", "k_dre_empire");

			var impCountries = new CountryCollection(new BufferedReader(" 1={tag=ROM} 2={tag=DRE}"));
			var titles = new Title.LandedTitles();
			titles.ImportImperatorCountries(impCountries,
				mapper,
				new LocalizationMapper(),
				new ProvinceMapper(),
				new CoaMapper(),
				new GovernmentMapper(),
				new SuccessionLawMapper(),
				new DefiniteFormMapper(),
				new ReligionMapper(),
				new CultureMapper(),
				new NicknameMapper(),
				new CharacterCollection(),
				new Date()
			);

			var apuliaGov = new Governorship(new BufferedReader("who=1 governorship=apulia_region"));
			var pepeGov = new Governorship(new BufferedReader("who=2 governorship=pepe_region"));
			var provinces = new ProvinceCollection();
			var imperatorRegionMapper = new ImperatorRegionMapper();
			var match = mapper.GetTitleForGovernorship(apuliaGov, impCountries[1], titles, provinces, imperatorRegionMapper);
			var match2 = mapper.GetTitleForGovernorship(pepeGov, impCountries[2], titles, provinces, imperatorRegionMapper);

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
		public void GetTitleGovernorshipTagReturnsNullOnCountryWithNoCK3Title() {
			var output = new StringWriter();
			Console.SetOut(output);

			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath);
			var country = new Country(1);
			var apuliaGov = new Governorship(new BufferedReader("who=1 governorship=apulia_region"));
			var match = mapper.GetTitleForGovernorship(apuliaGov, country, new Title.LandedTitles(), new ProvinceCollection(), new ImperatorRegionMapper());

			Assert.Null(match);
			Assert.Contains("[WARN] Country  has no associated CK3 title!", output.ToString());
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
			mapper.RegisterTag("BOR", "e_roman_empire");

			var impCountries = new CountryCollection(new BufferedReader(" 1={tag=BOR}"));
			var titles = new Title.LandedTitles();
			titles.ImportImperatorCountries(impCountries,
				mapper,
				new LocalizationMapper(),
				new ProvinceMapper(),
				new CoaMapper(),
				new GovernmentMapper(),
				new SuccessionLawMapper(),
				new DefiniteFormMapper(),
				new ReligionMapper(),
				new CultureMapper(),
				new NicknameMapper(),
				new CharacterCollection(),
				new Date()
			);

			var provinces = new ProvinceCollection();
			var imperatorRegionMapper = new ImperatorRegionMapper();

			mapper.RegisterGovernorship("aquitaine_region", "BOR", "k_atlantis");

			var aquitaneGov = new Governorship(new BufferedReader("who=1 governorship=aquitaine_region"));
			var match = mapper.GetTitleForGovernorship(aquitaneGov, impCountries[1], titles, provinces, imperatorRegionMapper);

			Assert.Equal("k_atlantis", match);
		}

		[Fact]
		public void GetCK3TitleRankReturnsCorrectRank() {
			var mapper = new TagTitleMapper(tagTitleMappingsPath, governorshipTitleMappingsPath);
			Assert.StartsWith("e", mapper.GetTitleForTag("TEST_TAG1", CountryRank.localPower, "Test Empire"));
			Assert.StartsWith("k", mapper.GetTitleForTag("TEST_TAG2", CountryRank.cityState, "Test Kingdom"));
			Assert.StartsWith("d", mapper.GetTitleForTag("TEST_TAG3", CountryRank.migrantHorde));
			Assert.StartsWith("d", mapper.GetTitleForTag("TEST_TAG4", CountryRank.cityState));
			Assert.StartsWith("k", mapper.GetTitleForTag("TEST_TAG5", CountryRank.localPower));
			Assert.StartsWith("k", mapper.GetTitleForTag("TEST_TAG6", CountryRank.regionalPower));
			Assert.StartsWith("k", mapper.GetTitleForTag("TEST_TAG7", CountryRank.majorPower));
			Assert.StartsWith("e", mapper.GetTitleForTag("TEST_TAG8", CountryRank.greatPower));
			Assert.StartsWith("e", mapper.GetTitleForTag("TEST_TAG8", CountryRank.greatPower));
		}
	}
}
