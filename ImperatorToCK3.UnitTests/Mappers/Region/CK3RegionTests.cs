using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.CK3.Titles;
using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Region {
	public class CK3RegionTests {
		[Fact]
		public void BlankRegionLoadsWithNoRegionsAndNoDuchies() {
			var reader = new BufferedReader(string.Empty);
			var region = CK3Region.Parse(reader);

			Assert.Empty(region.Regions);
			Assert.Empty(region.Duchies);
		}

		[Fact]
		public void AreaCanBeLoaded() {
			var reader = new BufferedReader("duchies = { d_ivrea } \n");
			var region = CK3Region.Parse(reader);

			Assert.Collection(region.Duchies,
				item => Assert.Equal("d_ivrea", item.Key)
			);
		}

		[Fact]
		public void RegionCanBeLoaded() {
			var reader = new BufferedReader("regions = { sicily_region }");
			var region = CK3Region.Parse(reader);

			Assert.Collection(region.Regions,
				item => Assert.Equal("sicily_region", item.Key)
			);
		}

		[Fact]
		public void MultipleDuchiesCanBeLoaded() {
			var reader = new BufferedReader("duchies = { d_ivrea d_athens d_oppo }");
			var region = CK3Region.Parse(reader);

			Assert.Equal(3, region.Duchies.Count);
		}

		[Fact]
		public void MultipleRegionsCanBeLoaded() {
			var reader = new BufferedReader(
				"regions = { sicily_region island_region new_region }");
			var region = CK3Region.Parse(reader);

			Assert.Equal(3, region.Regions.Count);
		}

		[Fact]
		public void RegionCanBeLinkedToDuchy() {
			var reader = new BufferedReader("duchies = { d_ivrea d_athens d_oppo }");
			var region = CK3Region.Parse(reader);

			var reader2 = new BufferedReader(
				"{ c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } }"
			);
			var duchy2 = new Title("d_athens");
			duchy2.LoadTitles(reader2);

			Assert.Null(region.Duchies["d_athens"]); // nullptr before linking
			region.LinkDuchy(duchy2);
			Assert.NotNull(region.Duchies["d_athens"]);
		}

		[Fact]
		public void LinkedRegionCanLocateProvince() {
			var reader = new BufferedReader("duchies = { d_ivrea d_athens d_oppo }");
			var region = CK3Region.Parse(reader);

			var reader2 = new BufferedReader(
				"= { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } }"
			);
			var duchy2 = new Title("d_athens");
			duchy2.LoadTitles(reader2);

			region.LinkDuchy(duchy2);

			Assert.True(region.ContainsProvince(79));
			Assert.True(region.ContainsProvince(56));
		}

		[Fact]
		public void LinkedRegionWillFailForProvinceMismatch() {
			var reader = new BufferedReader("duchies = { d_ivrea d_athens d_oppo }");
			var region = CK3Region.Parse(reader);

			var reader2 = new BufferedReader(
				"{ c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } }"
			);
			var duchy2 = new Title("d_athens");
			duchy2.LoadTitles(reader2);

			region.LinkDuchy(duchy2);

			Assert.False(region.ContainsProvince(7));
		}
	}
}
