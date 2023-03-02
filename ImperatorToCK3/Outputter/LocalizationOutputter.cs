using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Outputter;
public static class LocalizationOutputter {
	public static void OutputLocalization(ModFilesystem irModFS, string outputName, World ck3World) {
		var outputPath = Path.Combine("output", outputName);
		var baseLocDir = Path.Join(outputPath, "localization");
		var baseReplaceLocDir = Path.Join(baseLocDir, "replace");

		// copy character/family names localization
		foreach (var languageName in ConverterGlobals.SupportedLanguages) {
			var locFileLocation = irModFS.GetActualFileLocation($"localization/{languageName}/character_names_l_{languageName}.yml");
			if (locFileLocation is not null) {
				SystemUtils.TryCopyFile(locFileLocation,
					Path.Combine(outputPath, $"localization/replace/{languageName}/IMPERATOR_character_names_l_{languageName}.yml")
				);
			}
		}

		foreach (var language in ConverterGlobals.SupportedLanguages) {
			var locFilePath = Path.Join(baseReplaceLocDir, language, $"converter_l_{language}.yml");
			using var locFileStream = File.OpenWrite(locFilePath);
			using var locWriter = new StreamWriter(locFileStream, encoding: System.Text.Encoding.UTF8);

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
			using var dynastyLocStream = File.OpenWrite(dynastyLocFilePath);
			using var dynastyLocWriter = new StreamWriter(dynastyLocStream, System.Text.Encoding.UTF8);

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
	}
}
