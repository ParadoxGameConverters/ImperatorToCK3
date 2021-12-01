using commonItems;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;
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
			var area = new ImperatorArea(reader2);
			var areas = new Dictionary<string, ImperatorArea> { ["test1"] = area };
			region.LinkAreas(areas);

			Assert.NotNull(region.Areas["test1"]);
		}

		[Fact]
		public void MultipleAreasCanBeLoaded() {
			var reader = new BufferedReader("areas = { test1 test2 test3 }");
			var region = new ImperatorRegion("region1", reader);

			var emptyReader = new BufferedReader(string.Empty);
			var area1 = new ImperatorArea(emptyReader);
			var area2 = new ImperatorArea(emptyReader);
			var area3 = new ImperatorArea(emptyReader);
			var areas = new Dictionary<string, ImperatorArea> { ["test1"] = area1, ["test2"] = area2, ["test3"] = area3 };
			region.LinkAreas(areas);

			Assert.Collection(region.Areas,
				item => Assert.Equal("test1", item.Key),
				item => Assert.Equal("test2", item.Key),
				item => Assert.Equal("test3", item.Key)
			);
		}

		[Fact]
		public void LinkedRegionCanLocateProvince() {
			var reader1 = new BufferedReader("{ areas={area1} }");
			var region = new ImperatorRegion("region1", reader1);

			var reader2 = new BufferedReader("{ provinces = { 3 6 2 }}");
			var area = new ImperatorArea(reader2);
			var areas = new Dictionary<string, ImperatorArea> {["area1"] = area};
			region.LinkAreas(areas);

			Assert.True(region.ContainsProvince(6));
		}

		[Fact]
		public void LinkedRegionWillFailForProvinceMismatch() {
			var reader1 = new BufferedReader("{ areas={area1} }");
			var region = new ImperatorRegion("region1", reader1);

			var reader2 = new BufferedReader("{ provinces  = { 3 6 2 }}");
			var area = new ImperatorArea(reader2);
			var areas = new Dictionary<string, ImperatorArea> { ["area1"] = area };
			region.LinkAreas(areas);

			Assert.False(region.ContainsProvince(7));
		}
	}
}
