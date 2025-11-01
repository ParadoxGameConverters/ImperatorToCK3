using commonItems.Exceptions;
using commonItems.Mods;
using System;
using System.IO;
using System.Reflection;
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

	[Fact]
	public void VerifyCK3ModsPathThrowsWhenNotPointingToStandardModsDirectory() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempRoot);
		try {
			var config = new Configuration {
				CK3ModsPath = tempRoot
			};
			var verifyMethod = typeof(Configuration).GetMethod("VerifyCK3ModsPath", BindingFlags.Instance | BindingFlags.NonPublic);
			var exception = Assert.Throws<TargetInvocationException>(() => verifyMethod!.Invoke(config, null));
			var userError = Assert.IsType<UserErrorException>(exception.InnerException);
			var expectedSuffix = Path.Combine("Paradox Interactive", "Crusader Kings III", "mod");
			Assert.Contains(expectedSuffix, userError.Message);
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyCK3ModsPathAcceptsStandardDirectoryWithTrailingSeparator() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var modsPath = Path.Combine(tempRoot, "Paradox Interactive", "Crusader Kings III", "mod");
		Directory.CreateDirectory(modsPath);
		try {
			var config = new Configuration {
				CK3ModsPath = modsPath + Path.DirectorySeparatorChar
			};
			var verifyMethod = typeof(Configuration).GetMethod("VerifyCK3ModsPath", BindingFlags.Instance | BindingFlags.NonPublic);
			verifyMethod!.Invoke(config, null);
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}
}