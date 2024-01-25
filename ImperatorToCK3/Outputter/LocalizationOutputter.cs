using commonItems;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Outputter;
public static class LocalizationOutputter {
	public static void OutputLocalization(ModFilesystem irModFS, string outputName, World ck3World) {
		var outputPath = Path.Combine("output", outputName);
		var baseLocDir = Path.Join(outputPath, "localization");
		var baseReplaceLocDir = Path.Join(baseLocDir, "replace");

		CopyCharacterAndFamilyNamesLocalization(irModFS, outputPath);

		foreach (var language in ConverterGlobals.SupportedLanguages) {
			var locFilePath = Path.Join(baseReplaceLocDir, language, $"converter_l_{language}.yml");
			using var locWriter = FileOpeningHelper.OpenWriteWithRetries(locFilePath, encoding: System.Text.Encoding.UTF8);

			locWriter.WriteLine($"l_{language}:");

			// title localization
			foreach (var title in ck3World.LandedTitles) {
				foreach (var locBlock in title.Localizations) {
					locWriter.WriteLine(locBlock.GetYmlLocLineForLanguage(language));
				}
			}

			// character name localization
			var uniqueKeys = new HashSet<string>();
			foreach (var character in ck3World.Characters) {
				foreach (var (key, locBlock) in character.Localizations) {
					if (uniqueKeys.Contains(key)) {
						continue;
					}

					locWriter.WriteLine(locBlock.GetYmlLocLineForLanguage(language));
					uniqueKeys.Add(key);
				}
			}
		}

		// dynasty localization
		foreach (var language in ConverterGlobals.SupportedLanguages) {
			var dynastyLocFilePath = Path.Combine(baseLocDir, $"{language}/irtock3_dynasty_l_{language}.yml");
			using var dynastyLocWriter = FileOpeningHelper.OpenWriteWithRetries(dynastyLocFilePath, System.Text.Encoding.UTF8);

			dynastyLocWriter.WriteLine($"l_{language}:");

			foreach (var dynasty in ck3World.Dynasties) {
				var localizedName = dynasty.LocalizedName;
				if (localizedName is not null) {
					dynastyLocWriter.WriteLine(localizedName.GetYmlLocLineForLanguage(language));
				} else {
					Logger.Warn($"Dynasty {dynasty.Id} has no localizations!");
					dynastyLocWriter.WriteLine($" {dynasty.Name}: \"{dynasty.Name}\"");
				}
			}
		}
		
		OutputFallbackLockForMissingSecondaryLanguageLoc(baseLocDir, ck3World.ModFS);
	}

	private static void CopyCharacterAndFamilyNamesLocalization(ModFilesystem irModFS, string outputPath) {
		foreach (var languageName in ConverterGlobals.SupportedLanguages) {
			var locFileLocation = irModFS.GetActualFileLocation($"localization/{languageName}/character_names_l_{languageName}.yml");
			if (locFileLocation is not null) {
				SystemUtils.TryCopyFile(locFileLocation,
					Path.Combine(outputPath, $"localization/replace/{languageName}/IMPERATOR_character_names_l_{languageName}.yml")
				);
			}
		}
	}

	private static void OutputFallbackLockForMissingSecondaryLanguageLoc(string baseLocDir, ModFilesystem ck3ModFS) {
		var primaryLanguage = ConverterGlobals.PrimaryLanguage;
		var secondaryLanguages = ConverterGlobals.SecondaryLanguages;
		
		var ck3LocDB = new LocDB(primaryLanguage, secondaryLanguages);
		ck3LocDB.ScrapeLocalizations(ck3ModFS);

		var languageToLocLinesDict = new Dictionary<string, List<string>>();
		foreach (var language in secondaryLanguages) {
			languageToLocLinesDict[language] = new List<string>();
		}
		
		foreach (var locBlock in ck3LocDB) {
			if (!locBlock.HasLocForLanguage(primaryLanguage)) {
				continue;
			}

			foreach (var secondaryLanguage in secondaryLanguages) {
				if (locBlock.HasLocForLanguage(secondaryLanguage)) {
					continue;
				}
				
				languageToLocLinesDict[secondaryLanguage].Add(locBlock.GetYmlLocLineForLanguage(primaryLanguage));
			}
		}

		foreach (var language in secondaryLanguages) {
			var linesToOutput = languageToLocLinesDict[language];
			if (linesToOutput.Count == 0) {
				continue;
			}
			
			Logger.Debug($"Outputting {linesToOutput.Count} fallback loc lines for {language}...");
			
			var locFilePath = Path.Combine(baseLocDir, $"{language}/irtock3_fallback_loc_l_{language}.yml");
			using var locWriter = FileOpeningHelper.OpenWriteWithRetries(locFilePath, System.Text.Encoding.UTF8);

			locWriter.WriteLine($"l_{language}:");
			foreach (var line in linesToOutput) {
				locWriter.WriteLine(line);
			}
		}
	}
}
