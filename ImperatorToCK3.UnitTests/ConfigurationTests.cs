using commonItems.Mods;
using ImperatorToCK3.Exceptions;
using Xunit;

namespace ImperatorToCK3.UnitTests;

public class ConfigurationTests {
	[Fact]
	public void DetectSpecificCK3ModsThrowsExceptionForUnsupportedModCombinations() {
		const string tfeName = "The Fallen Eagle";
		const string wtwsmsName = "When the World Stopped Making Sense";
		const string roaName = "Rajas of Asia";
		const string aepName = "Asia Expansion Project";
		
		var tfeMod = new Mod(tfeName, "", dependencies: []);
		var wtwsmsMod = new Mod(wtwsmsName, "", dependencies: []);
		var roaMod = new Mod(roaName, "", dependencies: []);
		var aepMod = new Mod(aepName, "", dependencies: []);
		
		var ex = Assert.Throws<UserErrorException>(() => new Configuration().DetectSpecificCK3Mods([tfeMod, wtwsmsMod]));
		Assert.Equal("The converter doesn't support combining The Fallen Eagle with When the World Stopped Making Sense!",
			ex.Message);
		
		ex = Assert.Throws<UserErrorException>(() => new Configuration().DetectSpecificCK3Mods([roaMod, aepMod]));
		Assert.Equal("The converter doesn't support combining Rajas of Asia with Asia Expansion Project!",
			ex.Message);
		
		ex = Assert.Throws<UserErrorException>(() => new Configuration().DetectSpecificCK3Mods([tfeMod, roaMod]));
		Assert.Equal("The converter doesn't support combining The Fallen Eagle with Rajas of Asia!", ex.Message);
		
		ex = Assert.Throws<UserErrorException>(() => new Configuration().DetectSpecificCK3Mods([tfeMod, aepMod]));
		Assert.Equal("The converter doesn't support combining The Fallen Eagle with Asia Expansion Project!",
			ex.Message);
	}
}