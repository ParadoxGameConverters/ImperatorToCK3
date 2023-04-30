using ImperatorToCK3.CK3.Characters;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Characters; 

public class ColorDNAGeneValueTests {
	[Fact]
	public void DNAColorGeneValueIsInitialized() {
		var colorGeneValue = new DNAColorGeneValue {
			X = 1,
			Y = 2,
			XRecessive = 3,
			YRecessive = 4
		};
		Assert.Equal(1, colorGeneValue.X);
		Assert.Equal(2, colorGeneValue.Y);
		Assert.Equal(3, colorGeneValue.XRecessive);
		Assert.Equal(4, colorGeneValue.YRecessive);
	}
	
	[Fact]
	public void DNAColorGeneValueIsCorrectlyConvertedToString() {
		var colorGeneValue = new DNAColorGeneValue {
			X = 1,
			Y = 2,
			XRecessive = 3,
			YRecessive = 4
		};
		Assert.Equal("1 2 3 4", colorGeneValue.ToString());
	}
}