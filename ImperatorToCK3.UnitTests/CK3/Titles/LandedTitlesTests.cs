using commonItems;
using ImperatorToCK3.CK3.Titles;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Titles {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class LandedTitlesTests {
		[Fact]
		public void TitlesDefaultToEmpty() {
			var reader = new BufferedReader(string.Empty);
			var titles = new LandedTitles();
			titles.LoadTitles(reader);

			Assert.Empty(titles.StoredTitles);
		}

		[Fact]
		public void TitlesCanBeLoaded() {
			var reader = new BufferedReader(
				"b_barony = { province = 12 }\n" +
				"c_county = { landless = yes }\n"
			);

			var titles = new LandedTitles();
			titles.LoadTitles(reader);

			var barony = titles.StoredTitles["b_barony"];
			var county = titles.StoredTitles["c_county"];

			Assert.Equal(2, titles.StoredTitles.Count);
			Assert.Equal((ulong)12, barony.Province);
			Assert.True(county.Landless);
		}

		[Fact]
		public void TitlesCanBeLoadedRecursively() {
			var reader = new BufferedReader(
				"e_empire1 = { k_kingdom2 = { d_duchy3 = { b_barony4 = { province = 12 } } } }\n" +
				"c_county5 = { landless = yes }\n"
			);

			var titles = new LandedTitles();
			titles.LoadTitles(reader);

			var barony = titles.StoredTitles["b_barony4"];
			var county = titles.StoredTitles["c_county5"];

			Assert.Equal(5, titles.StoredTitles.Count);
			Assert.Equal((ulong)12, barony.Province);
			Assert.True(county.Landless);
		}

		[Fact]
		public void TitlesCanBeOverriddenByMods() {
			var reader = new BufferedReader(
				"e_empire1 = { k_kingdom2 = { d_duchy3 = { b_barony4 = { province = 12 } } } }\n" +
				"c_county5 = { landless = yes }\n"
			);

			var titles = new LandedTitles();
			titles.LoadTitles(reader);

			var reader2 = new BufferedReader(
				"b_barony4 = { province = 15 }\n" +
				"c_county5 = { landless = no }\n"
			);
			titles.LoadTitles(reader2);

			var barony = titles.StoredTitles["b_barony4"];
			var county = titles.StoredTitles["c_county5"];

			Assert.Equal(5, titles.StoredTitles.Count);
			Assert.Equal((ulong)15, barony.Province);
			Assert.False(county.Landless);
		}

		[Fact]
		public void TitlesCanBeAddedByMods() {
			var reader = new BufferedReader(
				"e_empire1 = { k_kingdom2 = { d_duchy3 = { b_barony4 = { province = 12 } } } }\n" +
				"c_county5 = { landless = yes }\n"
			);

			var titles = new LandedTitles();
			titles.LoadTitles(reader);

			var reader2 = new BufferedReader(
				"c_county5 = { landless = no }\n" + // Overrides existing instance
				"e_empire6 = { k_kingdom7 = { d_duchy8 = { b_barony9 = { province = 12 } } } }\n" +
				"c_county10 = { landless = yes }\n"
			);
			titles.LoadTitles(reader2);

			Assert.Equal(10, titles.StoredTitles.Count);
		}

		[Fact]
		public void CapitalsAreLinked() {
			var reader = new BufferedReader(
				"e_empire = { capital=c_county " +
				"k_kingdom = { d_duchy = { c_county = { b_barony = { province = 12 } } } } " +
				"}"
			);
			var titles = new LandedTitles();
			titles.LoadTitles(reader);

			var empire = titles.StoredTitles["e_empire"];
			var capitalCounty = empire.CapitalCounty;
			Assert.NotNull(capitalCounty);
			Assert.Equal("c_county", capitalCounty.Name);
			Assert.Equal("c_county", empire.CapitalCountyName);
		}
	}
}
