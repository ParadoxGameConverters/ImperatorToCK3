using commonItems;
using ImperatorToCK3.Mappers.Culture;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Culture {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class CultureMapperTests {
		[Fact]
		public void NonMatchGivesEmptyOptional() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = culture }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Null(culMapper.Match("nonMatchingCulture", "", 56, 49, "e_title"));
		}

		[Fact]
		public void SimpleCultureMatches() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = test }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Equal("culture", culMapper.Match("test", "", 56, 49, "e_title"));
		}

		[Fact]
		public void SimpleCultureCorrectlyMatches() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Equal("culture", culMapper.Match("test", "", 56, 49, "e_title"));
		}

		[Fact]
		public void CultureMatchesWithReligion() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Equal("culture", culMapper.Match("test", "thereligion", 56, 49, "e_title"));
		}

		[Fact]
		public void CultureFailsWithWrongReligion() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Null(culMapper.Match("test", "unreligion", 56, 49, "e_title"));
		}

		[Fact]
		public void CultureFailsWithNoReligion() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Null(culMapper.Match("test", "", 56, 49, "e_title"));
		}

		[Fact]
		public void CultureMatchesWithCapital() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4 }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Equal("culture", culMapper.Match("test", "thereligion", 4, 49, "e_title"));
		}

		[Fact]
		public void CultureFailsWithWrongCapital() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4 }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Null(culMapper.Match("test", "thereligion", 3, 49, "e_title"));
		}

		[Fact]
		public void CultureMatchesWithOwnerTitle() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4 owner = e_roman_empire }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Equal("culture", culMapper.Match("test", "thereligion", 4, 49, "e_roman_empire"));
		}

		[Fact]
		public void CultureFailsWithWrongOwnerTitle() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4 owner = e_roman_empire }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Null(culMapper.Match("test", "thereligion", 4, 49, "e_reman_empire"));
		}

		[Fact]
		public void CultureMatchesWithNoOwnerTitle() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4 }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Equal("culture", culMapper.Match("test", "thereligion", 4, 49, ""));
		}

		[Fact]
		public void CultureMatchesWithNoOwnerTitleInRule() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4}"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Equal("culture", culMapper.Match("test", "thereligion", 4, 49, "e_roman_empire"));
		}

		[Fact]
		public void CultureFailsWithOwnerTitleInRule() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion ck3Province = 4 owner = e_roman_empire }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Null(culMapper.Match("test", "thereligion", 4, 49, ""));
		}

		[Fact]
		public void NonMatchGivesEmptyOptionalWithNonReligiousMatch() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = culture }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Null(culMapper.NonReligiousMatch("nonMatchingCulture", "", 56, 49, "e_title"));
		}

		[Fact]
		public void SimpleCultureMatchesWithNonReligiousMatch() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = test }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Equal("culture", culMapper.NonReligiousMatch("test", "", 56, 49, "e_title"));
		}

		[Fact]
		public void SimpleCultureCorrectlyMatchesWithNonReligiousMatch() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Equal("culture", culMapper.NonReligiousMatch("test", "", 56, 49, "e_title"));
		}

		[Fact]
		public void CultureFailsWithCorrectReligionWithNonReligiousMatch() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Null(culMapper.NonReligiousMatch("test", "thereligion", 56, 49, "e_title"));
		}

		[Fact]
		public void CultureFailsWithWrongReligionWithNonReligiousMatch() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Null(culMapper.NonReligiousMatch("test", "unreligion", 56, 49, "e_title"));
		}

		[Fact]
		public void CultureFailsWithNoReligionWithNonReligiousMatch() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Null(culMapper.NonReligiousMatch("test", "", 56, 49, "e_title"));
		}

		[Fact]
		public void CultureMatchesWithReligionAndNonReligiousLinkWithNonReligiousMatch() {
			var reader = new BufferedReader(
				"link = { ck3 = culture imp = qwe imp = test imp = poi }"
			);
			var culMapper = new CultureMapper(reader);

			Assert.Equal("culture", culMapper.NonReligiousMatch("test", "thereligion", 56, 49, "e_title"));
		}

		[Fact]
		public void VariablesWorkInLinks() {
			var reader = new BufferedReader(
				"@germ_cultures = \"imp=sennonian imp=bellovacian imp=veliocassian imp=morinian\" \r\n" +
				"link = { ck3=low_germ @germ_cultures impProvince=1}\r\n" +
				"link = { ck3=high_germ @germ_cultures impProvince=2}"
			);
			var cultureMapper = new CultureMapper(reader);

			Assert.Null(cultureMapper.NonReligiousMatch("missing_culture", "", 0, impProvinceId: 1, ""));
			Assert.Equal("low_germ", cultureMapper.NonReligiousMatch("bellovacian", "", 0, impProvinceId: 1, ""));
			Assert.Equal("high_germ", cultureMapper.NonReligiousMatch("bellovacian", "", 0, impProvinceId: 2, ""));
		}
	}
}
