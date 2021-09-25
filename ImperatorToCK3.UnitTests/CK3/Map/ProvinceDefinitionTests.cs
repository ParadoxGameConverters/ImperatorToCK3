using ImperatorToCK3.CK3.Map;
using Xunit;
using ImageMagick;

namespace ImperatorToCK3.UnitTests.CK3.Map {
	public class ProvinceDefinitionTests {
		[Fact]
		public void DefinitionIsCorrectlyConstructed() {
			var def = new ProvinceDefinition(69, 250, 10, 40);
			Assert.Equal((ulong)69, def.Id);
			Assert.Equal(MagickColor.FromRgb(250,10,40), def.Color);
		}
	}
}
