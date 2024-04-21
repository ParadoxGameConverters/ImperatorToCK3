using commonItems;
using ImperatorToCK3.CommonUtils.Map;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Map;

[Collection("MapTests")]
[CollectionDefinition("MapTests", DisableParallelization = true)]
public class ProvincePositionTests {
	[Fact]
	public void PositionCanBeParsed() {
		const string blob = """
		{
			id = 5
			position ={ 271.722198 0.000000 3950.798096 }
			rotation ={ -0.000000 - 0.960029 - 0.000000 0.279900 }
			scale ={ 1.000000 1.000000 1.000000 }
		}
		""";
		var reader = new BufferedReader(blob);
		var pos = ProvincePosition.Parse(reader);
		Assert.Equal((ulong)5, pos.Id);
		Assert.Equal(271.722198, pos.X);
		Assert.Equal(3950.798096, pos.Y);
	}
}