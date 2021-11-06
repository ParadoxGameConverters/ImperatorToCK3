using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Provinces {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class ProvincesTests {
		[Fact]
		public void ProvincesDefaltToEmpty() {
			var provinces = new ImperatorToCK3.CK3.Provinces.Provinces();

			Assert.Empty(provinces.StoredProvinces);
		}

		[Fact]
		public void ProvincesAreProperlyLoadedFromFile() {
			var provinces = new ImperatorToCK3.CK3.Provinces.Provinces("TestFiles/CK3ProvincesHistoryFile.txt", new Date(867, 1, 1));

			Assert.Equal(4, provinces.StoredProvinces.Count);
			Assert.Equal("slovien", provinces.StoredProvinces[3080].Culture);
			Assert.Equal("catholic", provinces.StoredProvinces[3080].Religion);
			Assert.Equal("slovien", provinces.StoredProvinces[4165].Culture);
			Assert.Equal("catholic", provinces.StoredProvinces[4165].Religion);
			Assert.Equal("czech", provinces.StoredProvinces[4125].Culture);
			Assert.Equal("slavic_pagan", provinces.StoredProvinces[4125].Religion);
			Assert.Equal("czech", provinces.StoredProvinces[4161].Culture);
			Assert.Equal("slavic_pagan", provinces.StoredProvinces[4161].Religion);
		}
	}
}
