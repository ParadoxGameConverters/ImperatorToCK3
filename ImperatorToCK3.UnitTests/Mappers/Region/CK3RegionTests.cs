using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Region;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Region {
	public class CK3RegionTests {
		[Fact]
		public void BlankRegionLoadsWithNoRegionsAndNoDuchies() {
			var reader = new BufferedReader(string.Empty);
			var region = CK3Region.Parse("region1", reader);

			Assert.Empty(region.Regions);
			Assert.Empty(region.Duchies);
		}

		[Fact]
		public void DuchyCanBeLoaded() {
			var reader = new BufferedReader("duchies = { d_duchy }");
			var region = CK3Region.Parse("region1", reader);
			Assert.Empty(region.Duchies); // not linked yet

			var titles = new Title.LandedTitles();
			region.LinkDuchy(titles.Add("d_duchy"));
			Assert.Collection(region.Duchies,
				item => Assert.Equal("d_duchy", item.Key)
			);
		}

		[Fact]
		public void RegionCanBeLoaded() {
			var reader = new BufferedReader("regions = { sicily_region }");
			var region = CK3Region.Parse("region1", reader);
			Assert.Empty(region.Duchies); // not linked yet

			region.LinkRegion(new CK3Region("sicily_region"));
			Assert.Collection(region.Regions,
				item => Assert.Equal("sicily_region", item.Key)
			);
		}

		[Fact]
		public void MultipleDuchiesCanBeLoaded() {
			var reader = new BufferedReader("duchies = { d_ivrea d_athens d_oppo }");
			var region = CK3Region.Parse("region1", reader);
			Assert.Empty(region.Duchies); // not linked yet

			var titles = new Title.LandedTitles();
			region.LinkDuchy(titles.Add("d_ivrea"));
			region.LinkDuchy(titles.Add("d_athens"));
			region.LinkDuchy(titles.Add("d_oppo"));
			Assert.Equal(3, region.Duchies.Count);
		}

		[Fact]
		public void MultipleRegionsCanBeLoaded() {
			var reader = new BufferedReader(
				"regions = { sicily_region island_region new_region }");
			var region = CK3Region.Parse("region1", reader);

			Assert.Empty(region.Regions); // not linked yet

			region.LinkRegion(new CK3Region("sicily_region"));
			region.LinkRegion(new CK3Region("island_region"));
			region.LinkRegion(new CK3Region("new_region"));
			Assert.Equal(3, region.Regions.Count);
		}

		[Fact]
		public void RegionCanBeLinkedToDuchy() {
			var reader = new BufferedReader("duchies = { d_ivrea d_athens d_oppo }");
			var region = CK3Region.Parse("region1", reader);

			var titles = new Title.LandedTitles();
			var reader2 = new BufferedReader(
				"{ c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } }"
			);
			var duchy2 = titles.Add("d_athens");
			duchy2.LoadTitles(reader2);

			Assert.False(region.Duchies.ContainsKey("d_athens")); // not linked yet
			region.LinkDuchy(duchy2);
			Assert.NotNull(region.Duchies["d_athens"]);
		}

		[Fact]
		public void LinkedRegionCanLocateProvince() {
			var reader = new BufferedReader("duchies = { d_ivrea d_athens d_oppo }");
			var region = CK3Region.Parse("region1", reader);

			var titles = new Title.LandedTitles();
			var reader2 = new BufferedReader(
				"= { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } }"
			);
			var duchy2 = titles.Add("d_athens");
			duchy2.LoadTitles(reader2);

			region.LinkDuchy(duchy2);

			Assert.True(region.ContainsProvince(79));
			Assert.True(region.ContainsProvince(56));
		}

		[Fact]
		public void LinkedRegionWillFailForProvinceMismatch() {
			var reader = new BufferedReader("duchies = { d_ivrea d_athens d_oppo }");
			var region = CK3Region.Parse("region1", reader);

			var titles = new Title.LandedTitles();
			var reader2 = new BufferedReader(
				"{ c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } }"
			);
			var duchy2 = titles.Add("d_athens");
			duchy2.LoadTitles(reader2);

			region.LinkDuchy(duchy2);

			Assert.False(region.ContainsProvince(7));
		}
	}
}
