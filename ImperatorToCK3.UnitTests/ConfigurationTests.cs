using commonItems;
using commonItems.Exceptions;
using commonItems.Mods;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace ImperatorToCK3.UnitTests;

public class ConfigurationTests {
	[Fact]
	public void TrailingSlashesAreTrimmedFromProvidedPaths() {
		const string configurationPath = "configuration.txt";
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var imperatorPath = Path.Combine(tempRoot, "imperator");
		var imperatorDocPath = Path.Combine(tempRoot, "imperator_docs");
		var ck3Path = Path.Combine(tempRoot, "ck3");
		var ck3ModsPath = Path.Combine(tempRoot, "Paradox Interactive", "Crusader Kings III", "mod");

		Directory.CreateDirectory(Path.Combine(imperatorPath, "binaries"));
		Directory.CreateDirectory(Path.Combine(imperatorPath, "launcher"));
		Directory.CreateDirectory(Path.Combine(imperatorDocPath, "mod"));
		Directory.CreateDirectory(Path.Combine(ck3Path, "binaries"));
		Directory.CreateDirectory(Path.Combine(ck3Path, "launcher"));
		Directory.CreateDirectory(ck3ModsPath);

		var imperatorExeName = OperatingSystem.IsWindows() ? "imperator.exe" : "imperator";
		var ck3ExeName = OperatingSystem.IsWindows() ? "ck3.exe" : "ck3";
		File.WriteAllText(Path.Combine(imperatorPath, "binaries", imperatorExeName), "");
		File.WriteAllText(Path.Combine(ck3Path, "binaries", ck3ExeName), "");
		File.WriteAllText(Path.Combine(imperatorPath, "launcher", "launcher-settings.json"), "{\"version\":\"2.0.4\"}");
		File.WriteAllText(Path.Combine(ck3Path, "launcher", "launcher-settings.json"), "{\"version\":\"1.15.0\"}");

		var imperatorPathForConfig = imperatorPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		var imperatorDocPathForConfig = imperatorDocPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		var ck3PathForConfig = ck3Path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		var ck3ModsPathForConfig = ck3ModsPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		var imperatorPathWithTrailingSlash = imperatorPathForConfig + Path.AltDirectorySeparatorChar;
		var imperatorDocPathWithTrailingSlash = imperatorDocPathForConfig + Path.AltDirectorySeparatorChar;
		var ck3PathWithTrailingSlash = ck3PathForConfig + Path.AltDirectorySeparatorChar;
		var ck3ModsPathWithTrailingSlash = ck3ModsPathForConfig + Path.AltDirectorySeparatorChar;
		
		try {
			string content =
				$"ImperatorDirectory = \"{imperatorPathWithTrailingSlash}\"{Environment.NewLine}" +
				$"ImperatorDocDirectory = \"{imperatorDocPathWithTrailingSlash}\"{Environment.NewLine}" +
				$"CK3directory = \"{ck3PathWithTrailingSlash}\"{Environment.NewLine}" +
				$"targetGameModPath = \"{ck3ModsPathWithTrailingSlash}\"{Environment.NewLine}";

			File.WriteAllText(configurationPath, content);
			var config = new Configuration(new ConverterVersion());

			Assert.Equal(imperatorPathForConfig, config.ImperatorPath);
			Assert.Equal(imperatorDocPathForConfig, config.ImperatorDocPath);
			Assert.Equal(ck3PathForConfig, config.CK3Path);
			Assert.Equal(ck3ModsPathForConfig, config.CK3ModsPath);
		}
		finally {
			File.Delete(configurationPath);
			Directory.Delete(tempRoot, recursive: true);
		}
	}

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