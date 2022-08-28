using commonItems;
using ImperatorToCK3.Mappers.Region;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Region {
	public class ImperatorAreaTests {
		[Fact]
		public void BlankAreaLoadsWithNoProvinces() {
			var reader = new BufferedReader("");
			var area = new ImperatorArea("area", reader);

			Assert.Empty(area.ProvinceIds);
		}

		[Fact]
		public void ProvinceCanBeLoaded() {
			var reader = new BufferedReader("provinces = { 69 } \n");
			var area = new ImperatorArea("area", reader);

			Assert.Collection(area.ProvinceIds,
				item => Assert.Equal((ulong)69, item)
			);
		}

		[Fact]
		public void MultipleProvincesCanBeLoaded() {
			var reader = new BufferedReader("provinces = { 2 69 3 } \n");
			var area = new ImperatorArea("area", reader);

			Assert.Collection(area.ProvinceIds,
				item => Assert.Equal((ulong)2, item),
				item => Assert.Equal((ulong)3, item),
				item => Assert.Equal((ulong)69, item)
			);
		}

		[Fact]
		public void ContainsProvinceReturnsTrueForCorrectProvinces() {
			var reader = new BufferedReader("provinces = { 2 3 } \n");
			var area = new ImperatorArea("area", reader);

			Assert.True(area.ContainsProvince(2));
			Assert.True(area.ContainsProvince(3));
		}

		[Fact]
		public void ContainsProvinceReturnsFalseForMissingProvinces() {
			var reader = new BufferedReader("provinces = { 2 3 } \n");
			var area = new ImperatorArea("area", reader);

			Assert.False(area.ContainsProvince(4));
			Assert.False(area.ContainsProvince(5));
		}
	}
}
