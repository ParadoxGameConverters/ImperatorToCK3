using commonItems;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ImperatorToCK3.Outputter;
public static class LocalizationOutputter {
	public static void OutputLocalization(string outputModPath, World ck3World, LocDB locToOutputDB) {
		Logger.Info("Writing Localization...");
		var baseLocDir = Path.Join(outputModPath, "localization");
		var baseReplaceLocDir = Path.Join(baseLocDir, "replace");

		var sb = new StringBuilder();
		foreach (var language in ConverterGlobals.SupportedLanguages) {
			sb.AppendLine($"l_{language}:");
			
			foreach (var locBlock in locToOutputDB) {
				if (!locBlock.HasLocForLanguage(language)) {
					continue;
				}
				sb.AppendLine(locBlock.GetYmlLocLineForLanguage(language));
			}

			// title localization
			foreach (var title in ck3World.LandedTitles) {
				foreach (var locBlock in title.Localizations) {
					sb.AppendLine(locBlock.GetYmlLocLineForLanguage(language));
				}
			}

			// character name localization
			var uniqueKeys = new HashSet<string>();
			foreach (var character in ck3World.Characters) {
				foreach (var (key, locBlock) in character.Localizations) {
					if (uniqueKeys.Contains(key)) {
						continue;
					}

					sb.AppendLine(locBlock.GetYmlLocLineForLanguage(language));
					uniqueKeys.Add(key);
				}
			}
			
			var locFilePath = Path.Join(baseReplaceLocDir, language, $"converter_l_{language}.yml");
			using var locWriter = FileOpeningHelper.OpenWriteWithRetries(locFilePath, encoding: Encoding.UTF8);
			locWriter.WriteLine(sb.ToString());
			sb.Clear();
		}

		// dynasty localization
		foreach (var language in ConverterGlobals.SupportedLanguages) {
			sb.AppendLine($"l_{language}:");

			foreach (var dynasty in ck3World.Dynasties) {
				var localizedName = dynasty.LocalizedName;
				if (localizedName is not null) {
					sb.AppendLine(localizedName.GetYmlLocLineForLanguage(language));
				} else if (dynasty.FromImperator) {
					Logger.Warn($"Dynasty {dynasty.Id} has no localizations!");
					sb.AppendLine($" {dynasty.Name}: \"{dynasty.Name}\"");
				}
			}
			
			var dynastyLocFilePath = Path.Combine(baseLocDir, $"{language}/irtock3_dynasty_l_{language}.yml");
			using var dynastyLocWriter = FileOpeningHelper.OpenWriteWithRetries(dynastyLocFilePath, Encoding.UTF8);
			dynastyLocWriter.Write(sb.ToString());
			sb.Clear();
		}
	
		var alreadyWrittenLocDB = GetLocDBOfAlreadyWrittenLoc(baseLocDir, ck3World.ModFS);
		
		OutputOptionalLocFromConfigurables(baseLocDir, alreadyWrittenLocDB);
		OutputFallbackLocForMissingSecondaryLanguageLoc(baseLocDir, alreadyWrittenLocDB);
		
		Logger.IncrementProgress();
	}

	private static LocDB GetLocDBOfAlreadyWrittenLoc(string baseLocDir, ModFilesystem ck3ModFS) {
		// Read loc from CK3 and selected CK3 mods.
		var ck3LocDB = new LocDB(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);
		ck3LocDB.ScrapeLocalizations(ck3ModFS);

		// Also read already outputted loc from the output directory.
		var locFilesInOutputDir = Directory.GetFiles(baseLocDir, "*.yml", SearchOption.AllDirectories);
		foreach (var outputtedLocFilePath in locFilesInOutputDir) {
			ck3LocDB.ScrapeFile(outputtedLocFilePath);
		}
		
		return ck3LocDB;
	}

	private static Dictionary<string, HashSet<string>> GetDictOfLocPerLanguage(LocDB locDB) {
		var keysPerLanguage = new Dictionary<string, HashSet<string>>();
		foreach (var language in ConverterGlobals.SupportedLanguages) {
			keysPerLanguage[language] = [];
		}
	
		foreach (var locBlock in locDB) {
			foreach (var language in ConverterGlobals.SupportedLanguages) {
				if (locBlock.HasLocForLanguage(language)) {
					keysPerLanguage[language].Add(locBlock.Id);
				}
			}
		}
	
		return keysPerLanguage;
	}
	
	private static void OutputOptionalLocFromConfigurables(string baseLocDir, LocDB alreadyWrittenLocDB) {
		string optionalLocDir = "configurables/localization";
		if (!Directory.Exists(optionalLocDir)) {
			Logger.Warn("Optional loc directory not found, skipping optional loc output.");
			return;
		}
		Logger.Debug("Outputting optional loc...");
		var optionalLocDB = new LocDB(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);
		var optionalLocFilePaths = Directory.GetFiles(optionalLocDir, "*.yml", SearchOption.AllDirectories);
		foreach (var outputtedLocFilePath in optionalLocFilePaths) {
			optionalLocDB.ScrapeFile(outputtedLocFilePath);
		}
		
		var alreadyWrittenLocPerLanguage = GetDictOfLocPerLanguage(alreadyWrittenLocDB);
		
		foreach (var language in ConverterGlobals.SupportedLanguages) {
			var alreadyWrittenLocForLanguage = alreadyWrittenLocPerLanguage[language];
			
			var optionalLocLinesToOutput = new List<string>();

			foreach (var locBlock in optionalLocDB) {
				if (alreadyWrittenLocForLanguage.Contains(locBlock.Id)) {
					continue;
				}

				if (!locBlock.HasLocForLanguage(language)) {
					continue;
				}
				
				var loc = locBlock[language];
				if (loc is null) {
					continue;
				}
				
				optionalLocLinesToOutput.Add(locBlock.GetYmlLocLineForLanguage(language));
				alreadyWrittenLocDB.AddLocForKeyAndLanguage(locBlock.Id, language, loc);
			}
			
			if (optionalLocLinesToOutput.Count == 0) {
				continue;
			}
			
			Logger.Debug($"Outputting {optionalLocLinesToOutput.Count} optional loc lines for {language}...");
			var sb = new StringBuilder();
			sb.AppendLine($"l_{language}:");
			foreach (var line in optionalLocLinesToOutput) {
				sb.AppendLine(line);
			}
			
			var locFilePath = Path.Combine(baseLocDir, $"{language}/irtock3_optional_loc_l_{language}.yml");
			using var locWriter = FileOpeningHelper.OpenWriteWithRetries(locFilePath, Encoding.UTF8);
			locWriter.Write(sb.ToString());
		}
	}

	private static void OutputFallbackLocForMissingSecondaryLanguageLoc(string baseLocDir, LocDB ck3LocDB) {
		Logger.Debug("Outputting fallback loc for missing secondary language loc...");
		var primaryLanguage = ConverterGlobals.PrimaryLanguage;
		var secondaryLanguages = ConverterGlobals.SecondaryLanguages;
		
		var languageToLocLinesDict = new Dictionary<string, List<string>>();
		foreach (var language in secondaryLanguages) {
			languageToLocLinesDict[language] = [];
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
		
		var sb = new StringBuilder();
		foreach (var language in secondaryLanguages) {
			var linesToOutput = languageToLocLinesDict[language];
			if (linesToOutput.Count == 0) {
				continue;
			}
			
			Logger.Debug($"Outputting {linesToOutput.Count} fallback loc lines for {language}...");

			sb.AppendLine($"l_{language}:");
			foreach (var line in linesToOutput) {
				sb.AppendLine(line);
			}
			
			var locFilePath = Path.Combine(baseLocDir, $"{language}/irtock3_fallback_loc_l_{language}.yml");
			using var locWriter = FileOpeningHelper.OpenWriteWithRetries(locFilePath, Encoding.UTF8);
			locWriter.Write(sb.ToString());
			sb.Clear();
		}
	}
}
