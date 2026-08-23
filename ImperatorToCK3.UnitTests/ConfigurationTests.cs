using commonItems;
using commonItems.Exceptions;
using commonItems.Mods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace ImperatorToCK3.UnitTests;

[Collection("Sequential")]
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
	public void DetectSpecificCK3ModsSetsActiveFlagsFromConfigurable() {
		var tfeMod = new Mod("The Fallen Eagle v2.0", "", dependencies: []);

		var config = new Configuration();
		config.DetectSpecificCK3Mods([tfeMod]);

		Assert.True(config.FallenEagleEnabled);
		Assert.False(config.WhenTheWorldStoppedMakingSenseEnabled);
		Assert.False(config.RajasOfAsiaEnabled);
		Assert.False(config.AsiaExpansionProjectEnabled);
	}

	[Fact]
	public void DetectSpecificCK3ModsDetectsModById() {
		// Simulate a mod with a Steam workshop ID path but an unknown name.
		var tfeModByPath = new Mod("", "mod/ugc_2243307127.mod", dependencies: []);

		var config = new Configuration();
		config.DetectSpecificCK3Mods([tfeModByPath]);

		Assert.True(config.FallenEagleEnabled);
	}

	[Fact]
	public void GetCK3ModFlagsReturnsDynamicFlagsFromConfigurable() {
		var config = new Configuration();
		config.DetectSpecificCK3Mods([]);

		var flags = config.GetCK3ModFlags();

		// All flags from ck3_mods.txt should be present and false.
		Assert.True(flags.ContainsKey("tfe"));
		Assert.False(flags["tfe"]);
		Assert.True(flags.ContainsKey("wtwsms"));
		Assert.True(flags.ContainsKey("roa"));
		Assert.True(flags.ContainsKey("aep"));
		// Vanilla should be true when no mods are active.
		Assert.True(flags["vanilla_ck3"]);
	}

	[Fact]
	public void DetectSpecificImperatorModsSetsActiveFlagsFromConfigurable() {
		var invictusMod = new Mod("Imperator: Invictus 2.0", "", dependencies: []);

		var config = new Configuration();
		config.DetectSpecificImperatorMods([invictusMod]);

		Assert.True(config.InvictusDetected);
		Assert.False(config.Invictus1_7Detected);
		Assert.False(config.TerraIndomitaDetected);
	}

	[Fact]
	public void DetectSpecificImperatorModsDetectsInvictus1_7() {
		var invictus17Mod = new Mod("Imperator: Invictus 1.7.3", "", dependencies: []);

		var config = new Configuration();
		config.DetectSpecificImperatorMods([invictus17Mod]);

		Assert.True(config.InvictusDetected);
		Assert.True(config.Invictus1_7Detected);
	}

	[Fact]
	public void AddImperatorModFlagActivatesFlagForSaveDataFallback() {
		var config = new Configuration();
		config.DetectSpecificImperatorMods([]);

		Assert.False(config.InvictusDetected);
		config.AddImperatorModFlag("invictus");
		Assert.True(config.InvictusDetected);
	}

	[Fact]
	public void GetLiquidVariablesIncludesImperatorModFlags() {
		var invictusMod = new Mod("Imperator: Invictus 2.0", "", dependencies: []);

		var config = new Configuration();
		config.DetectSpecificImperatorMods([invictusMod]);

		var variables = config.GetLiquidVariables();

		Assert.True((bool)variables["invictus"]);
		Assert.False((bool)variables["invictus_1_7"]);
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

	private static MethodInfo GetPrivateMethod(string methodName) {
		return typeof(Configuration).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
	}

	private static object? InvokePrivateMethod(Configuration config, string methodName, object?[]? parameters = null) {
		return GetPrivateMethod(methodName).Invoke(config, parameters);
	}

	[Fact]
	public void SetSkipHoldingOwnersImportParsesYesAndNo() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetSkipHoldingOwnersImport");

		setter.Invoke(config, [new BufferedReader("yes")]);
		Assert.True(config.SkipHoldingOwnersImport);
		setter.Invoke(config, [new BufferedReader("no")]);
		Assert.False(config.SkipHoldingOwnersImport);
		setter.Invoke(config, [new BufferedReader("YES")]); // parsing is case-insensitive
		Assert.True(config.SkipHoldingOwnersImport);
	}

	[Fact]
	public void SetFillerDukesParsesDukeAndCount() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetFillerDukes");

		setter.Invoke(config, [new BufferedReader("duke")]);
		Assert.True(config.FillerDukes);
		setter.Invoke(config, [new BufferedReader("count")]);
		Assert.False(config.FillerDukes);
		setter.Invoke(config, [new BufferedReader("DUKE")]); // parsing is case-insensitive
		Assert.True(config.FillerDukes);
	}

	[Fact]
	public void SetStaticDeJureParsesStaticAndDynamic() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetStaticDeJure");

		setter.Invoke(config, [new BufferedReader("static")]);
		Assert.True(config.StaticDeJure);
		setter.Invoke(config, [new BufferedReader("dynamic")]);
		Assert.False(config.StaticDeJure);
		setter.Invoke(config, [new BufferedReader("STATIC")]); // parsing is case-insensitive
		Assert.True(config.StaticDeJure);
	}

	[Fact]
	public void SetHeresiesInHistoricalAreasParsesYesAndNo() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetHeresiesInHistoricalAreas");

		setter.Invoke(config, [new BufferedReader("yes")]);
		Assert.True(config.HeresiesInHistoricalAreas);
		setter.Invoke(config, [new BufferedReader("no")]);
		Assert.False(config.HeresiesInHistoricalAreas);
		setter.Invoke(config, [new BufferedReader("YES")]); // parsing is case-insensitive
		Assert.True(config.HeresiesInHistoricalAreas);
	}

	[Fact]
	public void SetUseCK3FlagsParsesYesAndNo() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetUseCK3Flags");

		setter.Invoke(config, [new BufferedReader("yes")]);
		Assert.True(config.UseCK3Flags);
		setter.Invoke(config, [new BufferedReader("no")]);
		Assert.False(config.UseCK3Flags);
		setter.Invoke(config, [new BufferedReader("YES")]); // parsing is case-insensitive
		Assert.True(config.UseCK3Flags);
	}

	[Fact]
	public void SetLegionConversionParsesAllValues() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetLegionConversion");

		setter.Invoke(config, [new BufferedReader("no")]);
		Assert.Equal(LegionConversion.No, config.LegionConversion);
		setter.Invoke(config, [new BufferedReader("special_troops")]);
		Assert.Equal(LegionConversion.SpecialTroops, config.LegionConversion);
		setter.Invoke(config, [new BufferedReader("men_at_arms")]);
		Assert.Equal(LegionConversion.MenAtArms, config.LegionConversion);
		setter.Invoke(config, [new BufferedReader("No")]); // parsing is case-insensitive
		Assert.Equal(LegionConversion.No, config.LegionConversion);

		// Unrecognized value: a warning is logged and the value is left unchanged.
		var output = new StringWriter();
		Console.SetOut(output);
		setter.Invoke(config, [new BufferedReader("bogus")]);
		Assert.Contains("[WARN] Failed to parse bogus as value for LegionConversion.", output.ToString());
		Assert.Equal(LegionConversion.No, config.LegionConversion);
	}

	[Fact]
	public void SetImperatorNomadsParsesAllValues() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetImperatorNomads");

		var expectedValues = new Dictionary<string, ImperatorNomads> {
			{ "only_steppe", ImperatorNomads.OnlySteppe },
			{ "leave_outside_convert_steppe_tribes", ImperatorNomads.LeaveOutsideConvertSteppeTribes },
			{ "none_outside_convert_steppe_tribes", ImperatorNomads.NoneOutsideConvertSteppeTribes },
			{ "no_nomads", ImperatorNomads.NoNomads },
			{ "no_changes", ImperatorNomads.NoChanges },
		};
		foreach (var (valueString, expectedValue) in expectedValues) {
			setter.Invoke(config, [new BufferedReader(valueString)]);
			Assert.Equal(expectedValue, config.ImperatorNomads);
		}
		setter.Invoke(config, [new BufferedReader("ONLY_STEPPE")]); // parsing is case-insensitive
		Assert.Equal(ImperatorNomads.OnlySteppe, config.ImperatorNomads);

		// Unrecognized value: a warning is logged and the value is left unchanged.
		var output = new StringWriter();
		Console.SetOut(output);
		setter.Invoke(config, [new BufferedReader("bogus")]);
		Assert.Contains("[WARN] Failed to parse bogus as value for ImperatorNomads.", output.ToString());
		Assert.Equal(ImperatorNomads.OnlySteppe, config.ImperatorNomads);
	}

	[Fact]
	public void SetFillerGovernmentsParsesAllValues() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetFillerGovernments");

		var expectedValues = new Dictionary<string, FillerGovernments> {
			{ "steppe_nomads_all", FillerGovernments.SteppeNomadsAll },
			{ "steppe_nomads_heritage", FillerGovernments.SteppeNomadsHeritage },
			{ "steppe_nomads_herdhead", FillerGovernments.SteppeNomadsHerdHead },
			{ "all_nomads", FillerGovernments.AllNomads },
			{ "no_changes", FillerGovernments.NoChanges },
		};
		foreach (var (valueString, expectedValue) in expectedValues) {
			setter.Invoke(config, [new BufferedReader(valueString)]);
			Assert.Equal(expectedValue, config.FillerGovernments);
		}
		setter.Invoke(config, [new BufferedReader("STEPPE_NOMADS_ALL")]); // parsing is case-insensitive
		Assert.Equal(FillerGovernments.SteppeNomadsAll, config.FillerGovernments);

		// Unrecognized value: a warning is logged and the value is left unchanged.
		var output = new StringWriter();
		Console.SetOut(output);
		setter.Invoke(config, [new BufferedReader("bogus")]);
		Assert.Contains("[WARN] Failed to parse bogus as value for FillerGovernments.", output.ToString());
		Assert.Equal(FillerGovernments.SteppeNomadsAll, config.FillerGovernments);
	}

	[Fact]
	public void SetMandalaRulersParsesAllValues() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetMandalaRulers");

		var expectedValues = new Dictionary<string, MandalaRulers> {
			{ "sea_feudal", MandalaRulers.SeaFeudal },
			{ "sea_nontribal", MandalaRulers.SeaNontribal },
			{ "sea_all", MandalaRulers.SeaAll },
			{ "everywhere_feudal", MandalaRulers.EverywhereFeudal },
			{ "everywhere_nontribal", MandalaRulers.EverywhereNontribal },
			{ "everywhere_all", MandalaRulers.EverywhereAll },
			{ "none", MandalaRulers.None },
		};
		foreach (var (valueString, expectedValue) in expectedValues) {
			setter.Invoke(config, [new BufferedReader(valueString)]);
			Assert.Equal(expectedValue, config.MandalaRulers);
		}
		setter.Invoke(config, [new BufferedReader("SEA_FEUDAL")]); // parsing is case-insensitive
		Assert.Equal(MandalaRulers.SeaFeudal, config.MandalaRulers);

		// Unrecognized value: a warning is logged and the value is left unchanged.
		var output = new StringWriter();
		Console.SetOut(output);
		setter.Invoke(config, [new BufferedReader("bogus")]);
		Assert.Contains("[WARN] Failed to parse bogus as value for MandalaRulers.", output.ToString());
		Assert.Equal(MandalaRulers.SeaFeudal, config.MandalaRulers);
	}

	[Fact]
	public void SetRitsuryoRulersParsesAllValues() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetRitsuryoRulers");

		var expectedValues = new Dictionary<string, RitsuryoRulers> {
			{ "japanese_japan", RitsuryoRulers.JapaneseJapan },
			{ "any_japan", RitsuryoRulers.AnyJapan },
			{ "none", RitsuryoRulers.None },
		};
		foreach (var (valueString, expectedValue) in expectedValues) {
			setter.Invoke(config, [new BufferedReader(valueString)]);
			Assert.Equal(expectedValue, config.RitsuryoRulers);
		}
		setter.Invoke(config, [new BufferedReader("JAPANESE_JAPAN")]); // parsing is case-insensitive
		Assert.Equal(RitsuryoRulers.JapaneseJapan, config.RitsuryoRulers);

		// Unrecognized value: a warning is logged and the value is left unchanged.
		var output = new StringWriter();
		Console.SetOut(output);
		setter.Invoke(config, [new BufferedReader("bogus")]);
		Assert.Contains("[WARN] Failed to parse bogus as value for RitsuryoRulers.", output.ToString());
		Assert.Equal(RitsuryoRulers.JapaneseJapan, config.RitsuryoRulers);
	}

	[Fact]
	public void SetBookmarkDateParsesValidDate() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetBookmarkDate");

		setter.Invoke(config, [new BufferedReader("867.1.1")]);

		Assert.Equal(new Date(867, 1, 1), config.CK3BookmarkDate);
	}

	[Fact]
	public void SetBookmarkDateIgnoresEmptyString() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetBookmarkDate");

		setter.Invoke(config, [new BufferedReader("\"\"")]);

		Assert.Equal(new Date(0, 1, 1), config.CK3BookmarkDate);
	}

	[Fact]
	public void SetBookmarkDateClampsTooEarlyDateAndLogsWarning() {
		var config = new Configuration();
		var setter = GetPrivateMethod("SetBookmarkDate");
		var output = new StringWriter();
		Console.SetOut(output);

		setter.Invoke(config, [new BufferedReader("1.1.1")]);

		Assert.Equal(new Date(2, 1, 1), config.CK3BookmarkDate);
		Assert.Contains("[WARN] CK3 bookmark date cannot be earlier than 2.1.1 AD (Y.M.D format)", output.ToString());
	}

	[Fact]
	public void VerifyImperatorPathThrowsWhenPathDoesNotExist() {
		var config = new Configuration {
			ImperatorPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivateMethod(config, "VerifyImperatorPath"));
		var userError = Assert.IsType<UserErrorException>(exception.InnerException);
		Assert.Contains("does not exist!", userError.Message);
	}

	[Fact]
	public void VerifyImperatorPathAcceptsInstallWithExecutable() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var imperatorPath = Path.Combine(tempRoot, "imperator");
		Directory.CreateDirectory(Path.Combine(imperatorPath, "binaries"));
		var imperatorExeName = OperatingSystem.IsWindows() ? "imperator.exe" : "imperator";
		File.WriteAllText(Path.Combine(imperatorPath, "binaries", imperatorExeName), "");
		try {
			var config = new Configuration { ImperatorPath = imperatorPath };
			InvokePrivateMethod(config, "VerifyImperatorPath");
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyImperatorPathAcceptsInstallWithSteamAppId() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var imperatorPath = Path.Combine(tempRoot, "imperator");
		Directory.CreateDirectory(Path.Combine(imperatorPath, "binaries"));
		File.WriteAllText(Path.Combine(imperatorPath, "binaries", "steam_appid.txt"), "859580");
		try {
			var config = new Configuration { ImperatorPath = imperatorPath };
			InvokePrivateMethod(config, "VerifyImperatorPath");
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyImperatorPathThrowsWhenExecutableAndSteamAppIdMissing() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var imperatorPath = Path.Combine(tempRoot, "imperator");
		Directory.CreateDirectory(Path.Combine(imperatorPath, "binaries"));
		try {
			var config = new Configuration { ImperatorPath = imperatorPath };

			var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivateMethod(config, "VerifyImperatorPath"));
			var userError = Assert.IsType<UserErrorException>(exception.InnerException);
			Assert.Contains("does not contain Imperator: Rome!", userError.Message);
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyCK3PathThrowsWhenPathDoesNotExist() {
		var config = new Configuration {
			CK3Path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivateMethod(config, "VerifyCK3Path"));
		var userError = Assert.IsType<UserErrorException>(exception.InnerException);
		Assert.Contains("does not exist!", userError.Message);
	}

	[Fact]
	public void VerifyCK3PathAcceptsInstallWithExecutable() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var ck3Path = Path.Combine(tempRoot, "ck3");
		Directory.CreateDirectory(Path.Combine(ck3Path, "binaries"));
		var ck3ExeName = OperatingSystem.IsWindows() ? "ck3.exe" : "ck3";
		File.WriteAllText(Path.Combine(ck3Path, "binaries", ck3ExeName), "");
		try {
			var config = new Configuration { CK3Path = ck3Path };
			InvokePrivateMethod(config, "VerifyCK3Path");
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyCK3PathAcceptsInstallWithSteamAppId() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var ck3Path = Path.Combine(tempRoot, "ck3");
		Directory.CreateDirectory(Path.Combine(ck3Path, "binaries"));
		File.WriteAllText(Path.Combine(ck3Path, "binaries", "steam_appid.txt"), "1158310");
		try {
			var config = new Configuration { CK3Path = ck3Path };
			InvokePrivateMethod(config, "VerifyCK3Path");
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyCK3PathThrowsWhenExecutableAndSteamAppIdMissing() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var ck3Path = Path.Combine(tempRoot, "ck3");
		Directory.CreateDirectory(Path.Combine(ck3Path, "binaries"));
		try {
			var config = new Configuration { CK3Path = ck3Path };

			var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivateMethod(config, "VerifyCK3Path"));
			var userError = Assert.IsType<UserErrorException>(exception.InnerException);
			Assert.Contains("does not contain Crusader Kings III!", userError.Message);
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyImperatorDocPathThrowsWhenPathDoesNotExist() {
		var config = new Configuration {
			ImperatorDocPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivateMethod(config, "VerifyImperatorDocPath"));
		var userError = Assert.IsType<UserErrorException>(exception.InnerException);
		Assert.Contains("does not exist!", userError.Message);
	}

	[Fact]
	public void VerifyImperatorDocPathAcceptsPathWithValidSubdirectory() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsPath = Path.Combine(tempRoot, "documents");
		Directory.CreateDirectory(Path.Combine(docsPath, "mod"));
		try {
			var config = new Configuration { ImperatorDocPath = docsPath };
			InvokePrivateMethod(config, "VerifyImperatorDocPath");
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyImperatorDocPathAcceptsPathWithValidFile() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsPath = Path.Combine(tempRoot, "documents");
		Directory.CreateDirectory(docsPath);
		File.WriteAllText(Path.Combine(docsPath, "pdx_settings.txt"), "");
		try {
			var config = new Configuration { ImperatorDocPath = docsPath };
			InvokePrivateMethod(config, "VerifyImperatorDocPath");
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyImperatorDocPathThrowsWhenPathContainsNoValidContent() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsPath = Path.Combine(tempRoot, "documents");
		Directory.CreateDirectory(docsPath);
		try {
			var config = new Configuration { ImperatorDocPath = docsPath };

			var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivateMethod(config, "VerifyImperatorDocPath"));
			var userError = Assert.IsType<UserErrorException>(exception.InnerException);
			Assert.Contains("is not a valid I:R documents path!", userError.Message);
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyCK3ModsPathThrowsWhenPathDoesNotExist() {
		var config = new Configuration {
			CK3ModsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "Paradox Interactive", "Crusader Kings III", "mod")
		};

		var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivateMethod(config, "VerifyCK3ModsPath"));
		var userError = Assert.IsType<UserErrorException>(exception.InnerException);
		Assert.Contains("does not exist!", userError.Message);
	}

	[Fact]
	public void VerifyCK3ModsPathThrowsWhenDirectoryContainsNoModFiles() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var modsPath = Path.Combine(tempRoot, "Paradox Interactive", "Crusader Kings III", "mod");
		Directory.CreateDirectory(modsPath);
		File.WriteAllText(Path.Combine(modsPath, "readme.txt"), "");
		try {
			var config = new Configuration { CK3ModsPath = modsPath };

			var exception = Assert.Throws<TargetInvocationException>(() => InvokePrivateMethod(config, "VerifyCK3ModsPath"));
			var userError = Assert.IsType<UserErrorException>(exception.InnerException);
			Assert.Contains("does not contain any .mod files!", userError.Message);
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyCK3ModsPathAcceptsDirectoryWithModFiles() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var modsPath = Path.Combine(tempRoot, "Paradox Interactive", "Crusader Kings III", "mod");
		Directory.CreateDirectory(modsPath);
		File.WriteAllText(Path.Combine(modsPath, "test_mod.mod"), "");
		try {
			var config = new Configuration { CK3ModsPath = modsPath };
			InvokePrivateMethod(config, "VerifyCK3ModsPath");
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	private static string CreateImperatorInstallWithLauncherVersion(string version) {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var imperatorPath = Path.Combine(tempRoot, "imperator");
		Directory.CreateDirectory(Path.Combine(imperatorPath, "launcher"));
		File.WriteAllText(Path.Combine(imperatorPath, "launcher", "launcher-settings.json"), $"{{\"version\":\"{version}\"}}");
		return imperatorPath;
	}

	private static string CreateCK3InstallWithLauncherVersion(string version) {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var ck3Path = Path.Combine(tempRoot, "ck3");
		Directory.CreateDirectory(Path.Combine(ck3Path, "launcher"));
		File.WriteAllText(Path.Combine(ck3Path, "launcher", "launcher-settings.json"), $"{{\"version\":\"{version}\"}}");
		return ck3Path;
	}

	[Fact]
	public void VerifyImperatorVersionIsLoadedFromLauncherFile() {
		var imperatorPath = CreateImperatorInstallWithLauncherVersion("2.0.4");
		try {
			var config = new Configuration { ImperatorPath = imperatorPath };
			InvokePrivateMethod(config, "VerifyImperatorVersion", [new ConverterVersion()]);

			Assert.Equal("2.0.4", config.IRVersion.ToShortString());
		}
		finally {
			Directory.Delete(Path.GetDirectoryName(imperatorPath)!, recursive: true);
		}
	}

	[Fact]
	public void VerifyImperatorVersionThrowsWhenBelowMinimum() {
		var imperatorPath = CreateImperatorInstallWithLauncherVersion("2.0.4");
		try {
			var converterVersion = new ConverterVersion();
			converterVersion.LoadVersion(new BufferedReader("minSource = \"3.0.0\" maxSource = \"9.9.9.9\""));
			var config = new Configuration { ImperatorPath = imperatorPath };

			var exception = Assert.Throws<TargetInvocationException>(() =>
				InvokePrivateMethod(config, "VerifyImperatorVersion", [converterVersion]));
			Assert.IsType<UserErrorException>(exception.InnerException);
		}
		finally {
			Directory.Delete(Path.GetDirectoryName(imperatorPath)!, recursive: true);
		}
	}

	[Fact]
	public void VerifyImperatorVersionThrowsWhenAboveMaximum() {
		var imperatorPath = CreateImperatorInstallWithLauncherVersion("2.0.4");
		try {
			var converterVersion = new ConverterVersion();
			converterVersion.LoadVersion(new BufferedReader("maxSource = \"1.0.0\""));
			var config = new Configuration { ImperatorPath = imperatorPath };

			var exception = Assert.Throws<TargetInvocationException>(() =>
				InvokePrivateMethod(config, "VerifyImperatorVersion", [converterVersion]));
			Assert.IsType<UserErrorException>(exception.InnerException);
		}
		finally {
			Directory.Delete(Path.GetDirectoryName(imperatorPath)!, recursive: true);
		}
	}

	[Fact]
	public void VerifyImperatorVersionLoadsVersionFromSullaBranchFile() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var imperatorPath = Path.Combine(tempRoot, "imperator");
		Directory.CreateDirectory(imperatorPath);
		File.WriteAllText(Path.Combine(imperatorPath, "sulla_branch.txt"), "release/1.2.3");
		try {
			var config = new Configuration { ImperatorPath = imperatorPath };
			InvokePrivateMethod(config, "VerifyImperatorVersion", [new ConverterVersion()]);

			Assert.Equal("1.2.3", config.IRVersion.ToShortString());
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyImperatorVersionThrowsWhenNoVersionSourceFound() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var imperatorPath = Path.Combine(tempRoot, "imperator");
		Directory.CreateDirectory(imperatorPath);
		try {
			var config = new Configuration { ImperatorPath = imperatorPath };

			var exception = Assert.Throws<TargetInvocationException>(() =>
				InvokePrivateMethod(config, "VerifyImperatorVersion", [new ConverterVersion()]));
			Assert.IsType<ConverterException>(exception.InnerException);
			Assert.Contains("Imperator version could not be determined.", exception.InnerException.Message);
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void GetImperatorVersionFromSullaBranchFileReturnsNullWhenFileMissing() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempRoot);
		try {
			var config = new Configuration { ImperatorPath = tempRoot };
			var output = new StringWriter();
			Console.SetOut(output);

			var result = InvokePrivateMethod(config, "GetImperatorVersionFromSullaBranchFile");

			Assert.Null(result);
			Assert.Contains("[WARN] sulla_branch.txt not found", output.ToString());
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void GetImperatorVersionFromSullaBranchFileReturnsVersionFromFile() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempRoot);
		File.WriteAllText(Path.Combine(tempRoot, "sulla_branch.txt"), "release/2.0.4");
		try {
			var config = new Configuration { ImperatorPath = tempRoot };

			var result = InvokePrivateMethod(config, "GetImperatorVersionFromSullaBranchFile");

			var version = (GameVersion)result!;
			Assert.Equal("2.0.4", version.ToShortString());
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyCK3VersionIsLoadedFromLauncherFile() {
		var ck3Path = CreateCK3InstallWithLauncherVersion("1.15.0");
		try {
			var config = new Configuration { CK3Path = ck3Path };
			InvokePrivateMethod(config, "VerifyCK3Version", [new ConverterVersion()]);

			Assert.Equal("1.15.0", config.CK3Version.ToShortString());
		}
		finally {
			Directory.Delete(Path.GetDirectoryName(ck3Path)!, recursive: true);
		}
	}

	[Fact]
	public void VerifyCK3VersionThrowsWhenBelowMinimum() {
		var ck3Path = CreateCK3InstallWithLauncherVersion("1.15.0");
		try {
			var converterVersion = new ConverterVersion();
			converterVersion.LoadVersion(new BufferedReader("minTarget = \"2.0.0\" maxTarget = \"9.9.9.9\""));
			var config = new Configuration { CK3Path = ck3Path };

			var exception = Assert.Throws<TargetInvocationException>(() =>
				InvokePrivateMethod(config, "VerifyCK3Version", [converterVersion]));
			Assert.IsType<UserErrorException>(exception.InnerException);
		}
		finally {
			Directory.Delete(Path.GetDirectoryName(ck3Path)!, recursive: true);
		}
	}

	[Fact]
	public void VerifyCK3VersionThrowsWhenAboveMaximum() {
		var ck3Path = CreateCK3InstallWithLauncherVersion("1.15.0");
		try {
			var converterVersion = new ConverterVersion();
			converterVersion.LoadVersion(new BufferedReader("maxTarget = \"1.0.0\""));
			var config = new Configuration { CK3Path = ck3Path };

			var exception = Assert.Throws<TargetInvocationException>(() =>
				InvokePrivateMethod(config, "VerifyCK3Version", [converterVersion]));
			Assert.IsType<UserErrorException>(exception.InnerException);
		}
		finally {
			Directory.Delete(Path.GetDirectoryName(ck3Path)!, recursive: true);
		}
	}

	[Fact]
	public void VerifyCK3VersionLoadsVersionFromTitusBranchFile() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var ck3Path = Path.Combine(tempRoot, "ck3");
		Directory.CreateDirectory(ck3Path);
		File.WriteAllText(Path.Combine(ck3Path, "titus_branch.txt"), "release/1.15.0");
		try {
			var config = new Configuration { CK3Path = ck3Path };
			InvokePrivateMethod(config, "VerifyCK3Version", [new ConverterVersion()]);

			Assert.Equal("1.15.0", config.CK3Version.ToShortString());
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void VerifyCK3VersionThrowsWhenNoVersionSourceFound() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var ck3Path = Path.Combine(tempRoot, "ck3");
		Directory.CreateDirectory(ck3Path);
		try {
			var config = new Configuration { CK3Path = ck3Path };

			var exception = Assert.Throws<TargetInvocationException>(() =>
				InvokePrivateMethod(config, "VerifyCK3Version", [new ConverterVersion()]));
			Assert.IsType<ConverterException>(exception.InnerException);
			Assert.Contains("CK3 version could not be determined.", exception.InnerException.Message);
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void GetCK3VersionFromTitusBranchFileReturnsNullWhenFileMissing() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempRoot);
		try {
			var config = new Configuration { CK3Path = tempRoot };
			var output = new StringWriter();
			Console.SetOut(output);

			var result = InvokePrivateMethod(config, "GetCK3VersionFromTitusBranchFile");

			Assert.Null(result);
			Assert.Contains("[WARN] titus_branch.txt not found", output.ToString());
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void GetCK3VersionFromTitusBranchFileReturnsVersionFromFile() {
		var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempRoot);
		File.WriteAllText(Path.Combine(tempRoot, "titus_branch.txt"), "release/1.15.0");
		try {
			var config = new Configuration { CK3Path = tempRoot };

			var result = InvokePrivateMethod(config, "GetCK3VersionFromTitusBranchFile");

			var version = (GameVersion)result!;
			Assert.Equal("1.15.0", version.ToShortString());
		}
		finally {
			Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void GetConverterOptionsSerializesAllOptionValues() {
		var config = new Configuration {
			HeresiesInHistoricalAreas = true,
			StaticDeJure = true,
			FillerDukes = false,
			UseCK3Flags = false,
			LegionConversion = LegionConversion.SpecialTroops,
			SkipDynamicCoAExtraction = true,
			SkipHoldingOwnersImport = false,
			ImperatorNomads = ImperatorNomads.LeaveOutsideConvertSteppeTribes,
			FillerGovernments = FillerGovernments.SteppeNomadsHeritage,
			MandalaRulers = MandalaRulers.SeaAll,
			RitsuryoRulers = RitsuryoRulers.None,
			ImperatorCurrencyRate = 1.5f,
			ImperatorCivilizationWorth = 0.25,
			CK3BookmarkDate = new Date(867, 1, 1),
		};

		var options = (OrderedDictionary<string, object>)InvokePrivateMethod(config, "GetConverterOptions")!;

		Assert.Equal("yes", options["HeresiesInHistoricalAreas"]);
		Assert.Equal("static", options["StaticDeJure"]);
		Assert.Equal("count", options["FillerDukes"]);
		Assert.Equal("no", options["UseCK3Flags"]);
		Assert.Equal("special_troops", options["LegionConversion"]);
		Assert.Equal("yes", options["SkipDynamicCoAExtraction"]);
		Assert.Equal("no", options["SkipHoldingOwnersImport"]);
		Assert.Equal("leave_outside_convert_steppe_tribes", options["ImperatorNomads"]);
		Assert.Equal("steppe_nomads_heritage", options["FillerGovernments"]);
		Assert.Equal("sea_all", options["MandalaRulers"]);
		Assert.Equal("none", options["RitsuryoRulers"]);
		Assert.Equal(1.5f, (float)options["ImperatorCurrencyRate"]!);
		Assert.Equal(0.25, (double)options["ImperatorCivilizationWorth"]!);
		Assert.Equal("0867-01-01", options["bookmark_date"]);
	}

	[Fact]
	public void GetConverterOptionsUsesDefaultValuesWhenOptionsNotSet() {
		var options = (OrderedDictionary<string, object>)InvokePrivateMethod(new Configuration(), "GetConverterOptions")!;

		Assert.Equal("no", options["HeresiesInHistoricalAreas"]);
		Assert.Equal("dynamic", options["StaticDeJure"]);
		Assert.Equal("duke", options["FillerDukes"]);
		Assert.Equal("yes", options["UseCK3Flags"]);
		Assert.Equal("men_at_arms", options["LegionConversion"]);
		Assert.Equal("no", options["SkipDynamicCoAExtraction"]);
		Assert.Equal("yes", options["SkipHoldingOwnersImport"]);
		Assert.Equal("only_steppe", options["ImperatorNomads"]);
		Assert.Equal("steppe_nomads_all", options["FillerGovernments"]);
		Assert.Equal("sea_feudal", options["MandalaRulers"]);
		Assert.Equal("japanese_japan", options["RitsuryoRulers"]);
		Assert.Equal(1.0f, (float)options["ImperatorCurrencyRate"]!);
		Assert.Equal(0.4, (double)options["ImperatorCivilizationWorth"]!);
		Assert.Equal("0000-01-01", options["bookmark_date"]);
	}

	[Fact]
	public void GetConverterOptionsSerializesEachEnumMember() {
		var config = new Configuration();

		var legionConversionValues = new Dictionary<LegionConversion, string> {
			{ LegionConversion.No, "no" },
			{ LegionConversion.SpecialTroops, "special_troops" },
			{ LegionConversion.MenAtArms, "men_at_arms" },
		};
		foreach (var (value, expectedString) in legionConversionValues) {
			config.LegionConversion = value;
			var options = (OrderedDictionary<string, object>)InvokePrivateMethod(config, "GetConverterOptions")!;
			Assert.Equal(expectedString, options["LegionConversion"]);
		}

		var imperatorNomadsValues = new Dictionary<ImperatorNomads, string> {
			{ ImperatorNomads.OnlySteppe, "only_steppe" },
			{ ImperatorNomads.LeaveOutsideConvertSteppeTribes, "leave_outside_convert_steppe_tribes" },
			{ ImperatorNomads.NoneOutsideConvertSteppeTribes, "none_outside_convert_steppe_tribes" },
			{ ImperatorNomads.NoNomads, "no_nomads" },
			{ ImperatorNomads.NoChanges, "no_changes" },
		};
		foreach (var (value, expectedString) in imperatorNomadsValues) {
			config.ImperatorNomads = value;
			var options = (OrderedDictionary<string, object>)InvokePrivateMethod(config, "GetConverterOptions")!;
			Assert.Equal(expectedString, options["ImperatorNomads"]);
		}

		var fillerGovernmentsValues = new Dictionary<FillerGovernments, string> {
			{ FillerGovernments.SteppeNomadsAll, "steppe_nomads_all" },
			{ FillerGovernments.SteppeNomadsHeritage, "steppe_nomads_heritage" },
			{ FillerGovernments.SteppeNomadsHerdHead, "steppe_nomads_herdhead" },
			{ FillerGovernments.AllNomads, "all_nomads" },
			{ FillerGovernments.NoChanges, "no_changes" },
		};
		foreach (var (value, expectedString) in fillerGovernmentsValues) {
			config.FillerGovernments = value;
			var options = (OrderedDictionary<string, object>)InvokePrivateMethod(config, "GetConverterOptions")!;
			Assert.Equal(expectedString, options["FillerGovernments"]);
		}

		var mandalaRulersValues = new Dictionary<MandalaRulers, string> {
			{ MandalaRulers.SeaFeudal, "sea_feudal" },
			{ MandalaRulers.SeaNontribal, "sea_nontribal" },
			{ MandalaRulers.SeaAll, "sea_all" },
			{ MandalaRulers.EverywhereFeudal, "everywhere_feudal" },
			{ MandalaRulers.EverywhereNontribal, "everywhere_nontribal" },
			{ MandalaRulers.EverywhereAll, "everywhere_all" },
			{ MandalaRulers.None, "none" },
		};
		foreach (var (value, expectedString) in mandalaRulersValues) {
			config.MandalaRulers = value;
			var options = (OrderedDictionary<string, object>)InvokePrivateMethod(config, "GetConverterOptions")!;
			Assert.Equal(expectedString, options["MandalaRulers"]);
		}

		var ritsuryoRulersValues = new Dictionary<RitsuryoRulers, string> {
			{ RitsuryoRulers.JapaneseJapan, "japanese_japan" },
			{ RitsuryoRulers.AnyJapan, "any_japan" },
			{ RitsuryoRulers.None, "none" },
		};
		foreach (var (value, expectedString) in ritsuryoRulersValues) {
			config.RitsuryoRulers = value;
			var options = (OrderedDictionary<string, object>)InvokePrivateMethod(config, "GetConverterOptions")!;
			Assert.Equal(expectedString, options["RitsuryoRulers"]);
		}
	}

	[Fact]
	public void GetConverterOptionsUsesFallbackValuesForUnknownEnumValues() {
		var config = new Configuration {
			LegionConversion = (LegionConversion)999,
			ImperatorNomads = (ImperatorNomads)999,
			FillerGovernments = (FillerGovernments)999,
			MandalaRulers = (MandalaRulers)999,
			RitsuryoRulers = (RitsuryoRulers)999,
		};

		var options = (OrderedDictionary<string, object>)InvokePrivateMethod(config, "GetConverterOptions")!;

		Assert.Equal("no", options["LegionConversion"]);
		Assert.Equal("only_steppe", options["ImperatorNomads"]);
		Assert.Equal("steppe_nomads_all", options["FillerGovernments"]);
		Assert.Equal("sea_feudal", options["MandalaRulers"]);
		Assert.Equal("japanese_japan", options["RitsuryoRulers"]);
	}
}