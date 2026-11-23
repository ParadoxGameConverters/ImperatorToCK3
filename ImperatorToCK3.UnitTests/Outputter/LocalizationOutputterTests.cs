using ImperatorToCK3;
using ImperatorToCK3.CK3;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class LocalizationOutputterTests {
	[Fact]
	public void FallbackLocIsGeneratedOnlyForSecondaryLanguagesMissingPrimaryKeyLoc() {
		var ck3LocDB = new TestCK3LocDB();
		ck3LocDB.AddLocForLanguage("key1", "english", "English loc 1");
		ck3LocDB.AddLocForLanguage("key1", "french", "French loc 1");
		ck3LocDB.AddLocForLanguage("key2", "english", "English loc 2");
		ck3LocDB.AddLocForLanguage("key3", "german", "German loc 3");

		var fallbackLocByLanguage = LocalizationOutputter.GetFallbackLocLinesByLanguage(ck3LocDB);

		Assert.DoesNotContain(" key1: \"English loc 1\"", fallbackLocByLanguage["french"]);
		Assert.Contains(" key1: \"English loc 1\"", fallbackLocByLanguage["german"]);
		Assert.Contains(" key2: \"English loc 2\"", fallbackLocByLanguage["french"]);
		Assert.Contains(" key2: \"English loc 2\"", fallbackLocByLanguage["german"]);
		Assert.DoesNotContain(" key3: \"\"", fallbackLocByLanguage["french"]);
		Assert.DoesNotContain(" key3: \"\"", fallbackLocByLanguage["german"]);
	}

	[Fact]
	public void OutputLocalizationWritesConverterLocForAllSupportedLanguages() {
		var tempDir = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempDir, "outputMod");
			Directory.CreateDirectory(Path.Combine(outputModPath, "localization", "replace", "english"));
			Directory.CreateDirectory(Path.Combine(outputModPath, "localization", "replace", "french"));
			Directory.CreateDirectory(Path.Combine(outputModPath, "localization", "german"));
			Directory.CreateDirectory(Path.Combine(outputModPath, "localization", "korean"));
			Directory.CreateDirectory(Path.Combine(outputModPath, "localization", "russian"));
			Directory.CreateDirectory(Path.Combine(outputModPath, "localization", "simp_chinese"));
			Directory.CreateDirectory(Path.Combine(outputModPath, "localization", "spanish"));
			Directory.CreateDirectory(Path.Combine(outputModPath, "localization", "french"));
			Directory.CreateDirectory(Path.Combine(outputModPath, "localization", "german"));

			var ck3LocDB = new TestCK3LocDB();
			ck3LocDB.AddLocForLanguage("test_key", "english", "English value");
			ck3LocDB.AddLocForLanguage("test_key", "french", "French value");

			var world = CreateWorldWithLocDB(ck3LocDB);

			LocalizationOutputter.OutputLocalization(outputModPath, world);

			var englishPath = Path.Combine(outputModPath, "localization", "replace", "english", "converter_l_english.yml");
			Assert.True(File.Exists(englishPath));
			var englishText = File.ReadAllText(englishPath);
			Assert.Contains("l_english:", englishText);
			Assert.Contains("test_key:", englishText);

			var frenchPath = Path.Combine(outputModPath, "localization", "replace", "french", "converter_l_french.yml");
			Assert.True(File.Exists(frenchPath));
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public void OutputLocalizationReturnsEarlyWhenNoEnglishLoc() {
		var tempDir = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempDir, "outputMod");
			Directory.CreateDirectory(Path.Combine(outputModPath, "localization", "replace", "english"));

			var ck3LocDB = new TestCK3LocDB();
			// Only secondary language loc, no english primary, so GetLocLinesForLanguage("english") will be 0
			ck3LocDB.AddLocForLanguage("only_french", "french", "French only");

			var world = CreateWorldWithLocDB(ck3LocDB);

			LocalizationOutputter.OutputLocalization(outputModPath, world);

			// Should return early and not create any converter file
			var englishPath = Path.Combine(outputModPath, "localization", "replace", "english", "converter_l_english.yml");
			Assert.False(File.Exists(englishPath));
			// Fallback should also not be called because early return skips it
			var fallbackPath = Path.Combine(outputModPath, "localization", "french", "irtock3_fallback_loc_l_french.yml");
			Assert.False(File.Exists(fallbackPath));
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public void OutputLocalizationWritesFallbackForMissingSecondary() {
		var tempDir = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempDir, "outputMod");
			foreach (var lang in new[] { "english", "french", "german", "korean", "russian", "simp_chinese", "spanish" }) {
				Directory.CreateDirectory(Path.Combine(outputModPath, "localization", "replace", lang));
				Directory.CreateDirectory(Path.Combine(outputModPath, "localization", lang));
			}

			var ck3LocDB = new TestCK3LocDB();
			// Add converter loc for all supported languages so OutputLocalization doesn't return early
			foreach (var lang in ConverterGlobals.SupportedLanguages) {
				ck3LocDB.AddLocForLanguage("converter_key", lang, $"{lang} value");
			}
			ck3LocDB.AddLocForLanguage("fallback_key", "english", "English fallback");

			var world = CreateWorldWithLocDB(ck3LocDB);

			LocalizationOutputter.OutputLocalization(outputModPath, world);

			// Fallback should be written for french (missing) but not for english
			var frenchFallback = Path.Combine(outputModPath, "localization", "french", "irtock3_fallback_loc_l_french.yml");
			Assert.True(File.Exists(frenchFallback));
			var frenchText = File.ReadAllText(frenchFallback);
			Assert.Contains("fallback_key:", frenchText);

			// No fallback file for english (primary) - should not exist or be empty
			var englishFallback = Path.Combine(outputModPath, "localization", "english", "irtock3_fallback_loc_l_english.yml");
			Assert.False(File.Exists(englishFallback));
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public void OutputLocalizationSkipsEmptyFallbackLanguages() {
		var tempDir = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempDir, "outputMod");
			foreach (var lang in ConverterGlobals.SupportedLanguages) {
				Directory.CreateDirectory(Path.Combine(outputModPath, "localization", "replace", lang));
				Directory.CreateDirectory(Path.Combine(outputModPath, "localization", lang));
			}

			var ck3LocDB = new TestCK3LocDB();
			foreach (var lang in ConverterGlobals.SupportedLanguages) {
				ck3LocDB.AddLocForLanguage("converter_key", lang, $"{lang} value");
				ck3LocDB.AddLocForLanguage("full_key", lang, $"{lang} full");
			}
			// No fallback needed because all secondary already have loc for full_key

			var world = CreateWorldWithLocDB(ck3LocDB);

			LocalizationOutputter.OutputLocalization(outputModPath, world);

			// No fallback files should be created because all secondary have loc
			foreach (var lang in ConverterGlobals.SecondaryLanguages) {
				var fallbackPath = Path.Combine(outputModPath, "localization", lang, $"irtock3_fallback_loc_l_{lang}.yml");
				Assert.False(File.Exists(fallbackPath));
			}
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	private static World CreateWorldWithLocDB(CK3LocDB locDB) {
		var world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
		var field = typeof(World).GetField("<LocDB>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		field!.SetValue(world, locDB);
		return world;
	}

	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "LocalizationOutputter", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static void TryDeleteDir(string dir) {
		try {
			if (Directory.Exists(dir)) {
				Directory.Delete(dir, recursive: true);
			}
		} catch {
			// Best effort
		}
	}
}