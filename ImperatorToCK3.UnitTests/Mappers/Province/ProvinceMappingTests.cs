using commonItems;
using ImperatorToCK3.Mappers.Province;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Province; 

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ProvinceMappingTests {
	[Fact]
	public void MappingDefaultsToEmpty() {
		var reader = new BufferedReader(string.Empty);
		var mapping = ProvinceMapping.Parse(reader);
		Assert.Empty(mapping.CK3Provinces);
		Assert.Empty(mapping.ImperatorProvinces);
	}
	[Fact]
	public void CK3ProvinceCanBeAdded() {
		var reader = new BufferedReader("= { ck3 = 2 ck3 = 1 }");
		var mapping = ProvinceMapping.Parse(reader);

		Assert.Equal((ulong)2, mapping.CK3Provinces[0]);
		Assert.Equal((ulong)1, mapping.CK3Provinces[1]);
	}

	[Fact]
	public void ImpProvinceCanBeAdded() {
		var reader = new BufferedReader("= { imp = 2 imp = 1 }");
		var mapping = ProvinceMapping.Parse(reader);

		Assert.Equal((ulong)2, mapping.ImperatorProvinces[0]);
		Assert.Equal((ulong)1, mapping.ImperatorProvinces[1]);
	}
}