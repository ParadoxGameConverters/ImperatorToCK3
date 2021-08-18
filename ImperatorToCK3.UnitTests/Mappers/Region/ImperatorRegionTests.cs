using commonItems;
using ImperatorToCK3.Mappers.Region;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Region {
	public class ImperatorRegionTests {
		[Fact]
		public void blankRegionLoadsWithNoAreas() {
			var reader = new BufferedReader(string.Empty);
			var region = new ImperatorRegion(reader);

			Assert.Empty(region.Areas);
		}

		[Fact]
		public void areaCanBeLoaded() {
			var reader = new BufferedReader("areas = { testarea } \n");
			var region = new ImperatorRegion(reader);

			Assert.Collection(region.Areas,
				item => Assert.Equal("testarea", item.Key)
			);
		}

		[Fact]
		public void multipleAreasCanBeLoaded() {
			var reader = new BufferedReader("areas = { test1 test2 test3 } \n");
			var region = new ImperatorRegion(reader);

			Assert.Collection(region.Areas,
				item => Assert.Equal("test1", item.Key),
				item => Assert.Equal("test2", item.Key),
				item => Assert.Equal("test3", item.Key)
			);
		}

		[Fact]
		public void regionCanBeLinkedToArea() {
			var reader1 = new BufferedReader("areas = { test1 test2 test3 } \n");
			var region = new ImperatorRegion(reader1);

			var reader2 = new BufferedReader("{ provinces  = { 3 6 2 }} \n");
			var area = new ImperatorArea(reader2);

			Assert.Null(region.Areas["test2"]); // null before linking
			region.LinkArea("test2", area);
			Assert.NotNull(region.Areas["test2"]);
		}

		[Fact]
		public void linkedRegionCanLocateProvince() {
			var reader1 = new BufferedReader(string.Empty);
			var region = new ImperatorRegion(reader1);

			var reader2 = new BufferedReader("{ provinces  = { 3 6 2 }} \n");
			var area = new ImperatorArea(reader2);

			region.LinkArea("test2", area);

			Assert.True(region.ContainsProvince(6));
		}

		[Fact]
		public void linkedRegionWillFailForProvinceMismatch() {
			var reader1 = new BufferedReader(string.Empty);
			var region = new ImperatorRegion(reader1);

			var reader2 = new BufferedReader("{ provinces  = { 3 6 2 }} \n");
			var area = new ImperatorArea(reader2);

			region.LinkArea("test2", area);

			Assert.False(region.ContainsProvince(7));
		}
	}
}
