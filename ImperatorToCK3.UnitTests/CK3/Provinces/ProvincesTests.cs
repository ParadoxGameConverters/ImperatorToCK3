using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Provinces {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class ProvincesTests {
		[Fact]
		public void ProvincesDefaultToEmpty() {
			var provinces = new ImperatorToCK3.CK3.Provinces.ProvinceCollection();

			Assert.Empty(provinces);
		}

		[Fact]
		public void ProvincesAreProperlyLoadedFromFile() {
			var provinces = new ImperatorToCK3.CK3.Provinces.ProvinceCollection("TestFiles/CK3ProvincesHistoryFile.txt", new Date(867, 1, 1));

			Assert.Equal(4, provinces.Count);
			Assert.Equal("slovien", provinces[3080].Culture);
			Assert.Equal("catholic", provinces[3080].Religion);
			Assert.Equal("slovien", provinces[4165].Culture);
			Assert.Equal("catholic", provinces[4165].Religion);
			Assert.Equal("czech", provinces[4125].Culture);
			Assert.Equal("slavic_pagan", provinces[4125].Religion);
			Assert.Equal("czech", provinces[4161].Culture);
			Assert.Equal("slavic_pagan", provinces[4161].Religion);
		}
	}
}
