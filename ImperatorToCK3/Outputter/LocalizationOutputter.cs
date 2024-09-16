using commonItems;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.Outputter;
public static class LocalizationOutputter {
	public static void OutputLocalization(string outputModPath, World ck3World) {
		Logger.Info("Writing Localization...");
		var baseLocDir = Path.Join(outputModPath, "localization");
		var baseReplaceLocDir = Path.Join(baseLocDir, "replace");

		foreach (var language in ConverterGlobals.SupportedLanguages) {
			var sb = new StringBuilder();
			var locLinesForLanguage = ck3World.LocDB.GetLocLinesToOutputForLanguage(language);
			if (locLinesForLanguage.Count == 0) {
				return;
			}

			sb.AppendLine($"l_{language}:");
			foreach (var line in locLinesForLanguage) {
				sb.AppendLine(line);
			}

			var locFilePath = Path.Join(baseReplaceLocDir, language, $"converter_l_{language}.yml");
			using var locWriter = FileHelper.OpenWriteWithRetries(locFilePath, encoding: Encoding.UTF8);
			locWriter.WriteLine(sb.ToString());
			sb.Clear();
		}
	
		OutputFallbackLocForMissingSecondaryLanguageLoc(baseLocDir, ck3World.LocDB);
		
		Logger.IncrementProgress();
	}

	private static void OutputFallbackLocForMissingSecondaryLanguageLoc(string baseLocDir, CK3LocDB ck3LocDB) {
		Logger.Debug("Outputting fallback loc for missing secondary language loc...");
		
		var languageToLocLinesDict = new Dictionary<string, List<string>>();
		foreach (var language in ConverterGlobals.SecondaryLanguages) {
			languageToLocLinesDict[language] = [];
		}

		var allLocKeys = ck3LocDB.Select(locBlock => locBlock.Id).Distinct().ToArray();
		
		foreach (var locKey in allLocKeys) {
			if (!ck3LocDB.HasKeyLocForLanguage(locKey, ConverterGlobals.PrimaryLanguage)) {
				continue;
			}

			foreach (var secondaryLanguage in ConverterGlobals.SecondaryLanguages) {
				if (ck3LocDB.HasKeyLocForLanguage(locKey, secondaryLanguage)) {
					continue;
				}

				var locLine = ck3LocDB.GetYmlLocLineForLanguage(locKey, ConverterGlobals.PrimaryLanguage);
				languageToLocLinesDict[secondaryLanguage].Add(locLine!);
			}
		}
		
		var sb = new StringBuilder();
		foreach (var language in ConverterGlobals.SecondaryLanguages) {
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
			using var locWriter = FileHelper.OpenWriteWithRetries(locFilePath, Encoding.UTF8);
			locWriter.Write(sb.ToString());
			sb.Clear();
		}
	}
}
