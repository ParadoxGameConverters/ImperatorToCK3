using commonItems;
using ImperatorToCK3.Mappers.Province;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.Province {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class ProvinceMapperTests {
		[Fact]
		public void EmptyMappingsDefaultToEmpty() {
			var reader = new BufferedReader(
			 "0.0.0.0 = {\n" +
			 "}"
			);
			var mapper = new ProvinceMapper(reader);

			Assert.Empty(mapper.GetImperatorProvinceNumbers(1));
		}

		[Fact]
		public void CanLookupImpProvinces() {
			var reader = new BufferedReader(
				"0.0.0.0 = {\n" +
				"	link = { ck3 = 2 ck3 = 1 imp = 2 imp = 1 }\n" +
				"}"
			);
			var mapper = new ProvinceMapper(reader);

			Assert.Equal(2, mapper.GetImperatorProvinceNumbers(1).Count);
			Assert.Equal((ulong)2, mapper.GetImperatorProvinceNumbers(1)[0]);
			Assert.Equal((ulong)1, mapper.GetImperatorProvinceNumbers(1)[1]);
			Assert.Equal(2, mapper.GetImperatorProvinceNumbers(2).Count);
			Assert.Equal((ulong)2, mapper.GetImperatorProvinceNumbers(2)[0]);
			Assert.Equal((ulong)1, mapper.GetImperatorProvinceNumbers(2)[1]);
		}

		[Fact]
		public void CanLookupCK3Provinces() {
			var reader = new BufferedReader(
				"0.0.0.0 = {\n" +
				"	link = { ck3 = 2 ck3 = 1 imp = 2 imp = 1 }\n" +
				"}"
			);
			var mapper = new ProvinceMapper(reader);

			Assert.Equal(2, mapper.GetCK3ProvinceNumbers(1).Count);
			Assert.Equal((ulong)2, mapper.GetCK3ProvinceNumbers(1)[0]);
			Assert.Equal((ulong)1, mapper.GetCK3ProvinceNumbers(1)[1]);
			Assert.Equal(2, mapper.GetCK3ProvinceNumbers(2).Count);
			Assert.Equal((ulong)2, mapper.GetCK3ProvinceNumbers(2)[0]);
			Assert.Equal((ulong)1, mapper.GetCK3ProvinceNumbers(2)[1]);
		}
	}
}
