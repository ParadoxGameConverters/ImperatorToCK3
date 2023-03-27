using commonItems;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.Exceptions;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Cultures; 

public class PillarTests {
	[Fact]
	public void ExceptionIsThrownOnMissingType() {
		var reader = new BufferedReader("= {}");
		var exception = Assert.Throws<ConverterException>(() => new Pillar("test_pillar", reader));
		Assert.Equal("Cultural pillar test_pillar has no type defined!", exception.Message);
	}
	
	[Fact]
	public void PillarIsCorrectlyInitialized() {
		var reader = new BufferedReader("= { type = test_type }");
		var pillar = new Pillar("test_pillar", reader);
		Assert.Equal("test_pillar", pillar.Id);
		Assert.Equal("test_type", pillar.Type);
	}
}