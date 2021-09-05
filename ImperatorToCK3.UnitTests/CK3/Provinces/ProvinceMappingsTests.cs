using ImperatorToCK3.CK3.Provinces;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Provinces {
	public class ProvinceMappingsTests {
		private const string testFilePath = "TestFiles/CK3_province_mappings.txt";

		[Fact]
		public void MappingsDefaultToEmpty() {
			var mappings = new ProvinceMappings("missingFile.txt");
			Assert.Empty(mappings.Mappings);
		}

		[Fact]
		public void MappingsCanBeLoadedFromFile() {
			var provinceMappings = new ProvinceMappings(testFilePath);
			var province1 = new Province(id: 1, new commonItems.BufferedReader(string.Empty));
			Assert.Collection(provinceMappings.Mappings,
				mapping1 => { Assert.Equal((ulong)3, mapping1.Key); Assert.Equal((ulong)1, mapping1.Value); },
				mapping1 => { Assert.Equal((ulong)4, mapping1.Key); Assert.Equal((ulong)1, mapping1.Value); },
				mapping1 => { Assert.Equal((ulong)5, mapping1.Key); Assert.Equal((ulong)4, mapping1.Value); }
			);
		}
	}
}
