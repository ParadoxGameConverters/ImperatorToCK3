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
	public static void OutputLocalization(string outputModPath, World ck3World) {
		Logger.Info("Writing Localization...");
		var baseLocDir = Path.Join(outputModPath, "localization");
		var baseReplaceLocDir = Path.Join(baseLocDir, "replace");

		var sb = new StringBuilder();
		foreach (var language in ConverterGlobals.SupportedLanguages) {
			var locLinesForLanguage = ck3World.LocDB.GetLocLinesToOutputForLanguage(language);
			if (locLinesForLanguage.Count == 0) {
				continue;
			}
			
			sb.AppendLine($"l_{language}:");
			foreach (var line in locLinesForLanguage) {
				sb.AppendLine(line);
			}
			
			var locFilePath = Path.Join(baseReplaceLocDir, language, $"converter_l_{language}.yml");
			using var locWriter = FileOpeningHelper.OpenWriteWithRetries(locFilePath, encoding: Encoding.UTF8);
			locWriter.WriteLine(sb.ToString());
			sb.Clear();
		}
	
		var alreadyWrittenLocDB = GetLocDBOfAlreadyWrittenLoc(baseLocDir, ck3World.ModFS);
		
		OutputOptionalLocFromConfigurables(baseLocDir, alreadyWrittenLocDB); // TODO: remove when refactor is done
		OutputFallbackLocForMissingSecondaryLanguageLoc(baseLocDir, alreadyWrittenLocDB);
		
		Logger.IncrementProgress();
	}
	
	private static void OutputOptionalLocFromConfigurables(string baseLocDir, CK3LocDB ck3LocDB) {
		Logger.Debug("Outputting optional loc...");
		
		foreach (var language in ConverterGlobals.SupportedLanguages) {
			var alreadyWrittenLocForLanguage = ck3LocDB.GetAlreadyOutputtedLocKeysForLanguage(language);
			
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
