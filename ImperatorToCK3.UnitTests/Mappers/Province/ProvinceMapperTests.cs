using ImperatorToCK3.Mappers.Province;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Province;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ProvinceMapperTests {
	private const string TestFilesPath = "TestFiles/MapperTests/ProvinceMapper";

	[Fact]
	public void EmptyMappingsDefaultToEmpty() {
		var mapper = new ProvinceMapper();
		var mappingsPath = Path.Combine(TestFilesPath, "empty.txt");
		mapper.LoadMappings(mappingsPath);

		Assert.Empty(mapper.GetImperatorProvinceNumbers(1));
	}

	[Fact]
	public void CanLookupImperatorProvinces() {
		var mapper = new ProvinceMapper();
		var mappingsPath = Path.Combine(TestFilesPath, "many_to_many.txt");
		mapper.LoadMappings(mappingsPath);

		Assert.Equal(2, mapper.GetImperatorProvinceNumbers(1).Count);
		Assert.Equal((ulong)2, mapper.GetImperatorProvinceNumbers(1)[0]);
		Assert.Equal((ulong)1, mapper.GetImperatorProvinceNumbers(1)[1]);
		Assert.Equal(2, mapper.GetImperatorProvinceNumbers(2).Count);
		Assert.Equal((ulong)2, mapper.GetImperatorProvinceNumbers(2)[0]);
		Assert.Equal((ulong)1, mapper.GetImperatorProvinceNumbers(2)[1]);
	}

	[Fact]
	public void CanLookupCK3Provinces() {
		var mapper = new ProvinceMapper();
		var mappingsPath = Path.Combine(TestFilesPath, "many_to_many.txt");
		mapper.LoadMappings(mappingsPath);

		Assert.Equal(2, mapper.GetCK3ProvinceNumbers(1).Count);
		Assert.Equal((ulong)2, mapper.GetCK3ProvinceNumbers(1)[0]);
		Assert.Equal((ulong)1, mapper.GetCK3ProvinceNumbers(1)[1]);
		Assert.Equal(2, mapper.GetCK3ProvinceNumbers(2).Count);
		Assert.Equal((ulong)2, mapper.GetCK3ProvinceNumbers(2)[0]);
		Assert.Equal((ulong)1, mapper.GetCK3ProvinceNumbers(2)[1]);
	}
}