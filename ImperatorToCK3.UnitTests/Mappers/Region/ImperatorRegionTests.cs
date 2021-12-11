using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Mappers.Region;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Region {
	public class ImperatorRegionTests {
		[Fact]
		public void BlankRegionLoadsWithNoAreas() {
			var reader = new BufferedReader(string.Empty);
			var region = new ImperatorRegion("region1", reader);

			Assert.Empty(region.Areas);
		}

		[Fact]
		public void RegionCanBeLinkedToArea() {
			var reader1 = new BufferedReader("areas = { test1 }");
			var region = new ImperatorRegion("region1", reader1);

			var reader2 = new BufferedReader("{ provinces  = { 3 6 2 }}");
			var area = new ImperatorArea("test1", reader2);
			var areas = new IdObjectCollection<string, ImperatorArea> { area };
			region.LinkAreas(areas);

			Assert.NotNull(region.Areas["test1"]);
		}

		[Fact]
		public void MultipleAreasCanBeLoaded() {
			var reader = new BufferedReader("areas = { test1 test2 test3 }");
			var region = new ImperatorRegion("region1", reader);

			var emptyReader = new BufferedReader(string.Empty);
			var area1 = new ImperatorArea("test1", emptyReader);
			var area2 = new ImperatorArea("test2", emptyReader);
			var area3 = new ImperatorArea("test3", emptyReader);
			var areas = new IdObjectCollection<string, ImperatorArea> { area1, area2, area3 };
			region.LinkAreas(areas);

			Assert.Collection(region.Areas,
				item => Assert.Equal("test1", item.Id),
				item => Assert.Equal("test2", item.Id),
				item => Assert.Equal("test3", item.Id)
			);
		}

		[Fact]
		public void LinkedRegionCanLocateProvince() {
			var reader1 = new BufferedReader("{ areas={area1} }");
			var region = new ImperatorRegion("region1", reader1);

			var reader2 = new BufferedReader("{ provinces = { 3 6 2 }}");
			var area = new ImperatorArea("area1", reader2);
			var areas = new IdObjectCollection<string, ImperatorArea> { area };
			region.LinkAreas(areas);

			Assert.True(region.ContainsProvince(6));
		}

		[Fact]
		public void LinkedRegionWillFailForProvinceMismatch() {
			var reader1 = new BufferedReader("{ areas={area1} }");
			var region = new ImperatorRegion("region1", reader1);

			var reader2 = new BufferedReader("{ provinces  = { 3 6 2 }}");
			var area = new ImperatorArea("area1", reader2);
			var areas = new IdObjectCollection<string, ImperatorArea> { area };
			region.LinkAreas(areas);

			Assert.False(region.ContainsProvince(7));
		}
	}
}
