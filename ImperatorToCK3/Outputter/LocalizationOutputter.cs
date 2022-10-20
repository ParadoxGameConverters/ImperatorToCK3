using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.Outputter;
public static class LocalizationOutputter {
	public static void OutputLocalization(ModFilesystem irModFS, string outputName, World ck3World) {
		// copy character/family names localization
		var outputPath = Path.Combine("output", outputName);
		IEnumerable<string> languageNames = new[] {"english", "french", "german", "russian", "simp_chinese", "spanish"};
		foreach (var languageName in languageNames) {
			var locFileLocation = irModFS.GetActualFileLocation($"localization/{languageName}/character_names_l_{languageName}.yml");
			if (locFileLocation is not null) {
				SystemUtils.TryCopyFile(locFileLocation,
					Path.Combine(outputPath, $"localization/replace/{languageName}/IMPERATOR_character_names_l_{languageName}.yml")
				);
			}
		}

		using var englishStream = File.OpenWrite("output/" + outputName + "/localization/replace/english/converter_l_english.yml");
		using var frenchStream = File.OpenWrite("output/" + outputName + "/localization/replace/french/converter_l_french.yml");
		using var germanStream = File.OpenWrite("output/" + outputName + "/localization/replace/german/converter_l_german.yml");
		using var koreanStream = File.OpenWrite("output/" + outputName + "/localization/replace/korean/converter_l_korean.yml");
		using var russianStream = File.OpenWrite("output/" + outputName + "/localization/replace/russian/converter_l_russian.yml");
		using var simpChineseStream = File.OpenWrite("output/" + outputName + "/localization/replace/simp_chinese/converter_l_simp_chinese.yml");
		using var spanishStream = File.OpenWrite("output/" + outputName + "/localization/replace/spanish/converter_l_spanish.yml");
		using var english = new StreamWriter(englishStream, System.Text.Encoding.UTF8);
		using var french = new StreamWriter(frenchStream, System.Text.Encoding.UTF8);
		using var german = new StreamWriter(germanStream, System.Text.Encoding.UTF8);
		using var korean = new StreamWriter(koreanStream, System.Text.Encoding.UTF8);
		using var russian = new StreamWriter(russianStream, System.Text.Encoding.UTF8);
		using var simpChinese = new StreamWriter(simpChineseStream, System.Text.Encoding.UTF8);
		using var spanish = new StreamWriter(spanishStream, System.Text.Encoding.UTF8);

		english.WriteLine("l_english:");
		french.WriteLine("l_french:");
		german.WriteLine("l_german:");
		korean.WriteLine("l_korean:");
		russian.WriteLine("l_russian:");
		simpChinese.WriteLine("l_simp_chinese:");
		spanish.WriteLine("l_spanish:");

		// title localization
		foreach (var title in ck3World.LandedTitles) {
			foreach (var loc in title.Localizations) {
				var key = loc.Id;
				english.WriteLine($" {key}: \"{loc["english"]}\"");
				french.WriteLine($" {key}: \"{loc["french"]}\"");
				german.WriteLine($" {key}: \"{loc["german"]}\"");
				korean.WriteLine($" {key}: \"{loc["korean"]}\"");
				russian.WriteLine($" {key}: \"{loc["russian"]}\"");
				simpChinese.WriteLine($" {key}: \"{loc["simp_chinese"]}\"");
				spanish.WriteLine($" {key}: \"{loc["spanish"]}\"");
			}
		}

		// character name localization
		var uniqueKeys = new HashSet<string>();
		foreach (var character in ck3World.Characters) {
			foreach (var (key, loc) in character.Localizations) {
				if (!uniqueKeys.Contains(key)) {
					english.WriteLine($" {key}: \"{loc["english"]}\"");
					french.WriteLine($" {key}: \"{loc["french"]}\"");
					german.WriteLine($" {key}: \"{loc["german"]}\"");
					korean.WriteLine($" {key}: \"{loc["korean"]}\"");
					russian.WriteLine($" {key}: \"{loc["russian"]}\"");
					simpChinese.WriteLine($" {key}: \"{loc["simp_chinese"]}\"");
					spanish.WriteLine($" {key}: \"{loc["spanish"]}\"");

					uniqueKeys.Add(key);
				}
			}
		}

		// dynasty localization
		using var englishDynLocStream = File.OpenWrite("output/" + outputName + "/localization/replace/english/imp_dynasty_l_english.yml");
		using var frenchDynLocStream = File.OpenWrite("output/" + outputName + "/localization/replace/french/imp_dynasty_l_french.yml");
		using var germanDynLocStream = File.OpenWrite("output/" + outputName + "/localization/replace/german/imp_dynasty_l_german.yml");
		using var koreanDynLocStream = File.OpenWrite("output/" + outputName + "/localization/replace/korean/imp_dynasty_l_korean.yml");
		using var russianDynLocStream = File.OpenWrite("output/" + outputName + "/localization/replace/russian/imp_dynasty_l_russian.yml");
		using var simp_chineseDynLocStream = File.OpenWrite("output/" + outputName + "/localization/replace/simp_chinese/imp_dynasty_l_simp_chinese.yml");
		using var spanishDynLocStream = File.OpenWrite("output/" + outputName + "/localization/replace/spanish/imp_dynasty_l_spanish.yml");
		using var englishDynLoc = new StreamWriter(englishDynLocStream, System.Text.Encoding.UTF8);
		using var frenchDynLoc = new StreamWriter(frenchDynLocStream, System.Text.Encoding.UTF8);
		using var germanDynLoc = new StreamWriter(germanDynLocStream, System.Text.Encoding.UTF8);
		using var koreanDynLoc = new StreamWriter(koreanDynLocStream, System.Text.Encoding.UTF8);
		using var russianDynLoc = new StreamWriter(russianDynLocStream, System.Text.Encoding.UTF8);
		using var simp_chineseDynLoc = new StreamWriter(simp_chineseDynLocStream, System.Text.Encoding.UTF8);
		using var spanishDynLoc = new StreamWriter(spanishDynLocStream, System.Text.Encoding.UTF8);

		englishDynLoc.WriteLine("l_english:");
		frenchDynLoc.WriteLine("l_french:");
		germanDynLoc.WriteLine("l_german:");
		koreanDynLoc.WriteLine("l_korean:");
		russianDynLoc.WriteLine("l_russian:");
		simp_chineseDynLoc.WriteLine("l_simp_chinese:");
		spanishDynLoc.WriteLine("l_spanish:");

		foreach (var dynasty in ck3World.Dynasties) {
			var (key, loc) = dynasty.Localization;
			englishDynLoc.WriteLine($" {key}: \"{loc["english"]}\"");
			frenchDynLoc.WriteLine($" {key}: \"{loc["french"]}\"");
			germanDynLoc.WriteLine($" {key}: \"{loc["german"]}\"");
			koreanDynLoc.WriteLine($" {key}: \"{loc["korean"]}\"");
			russianDynLoc.WriteLine($" {key}: \"{loc["russian"]}\"");
			simp_chineseDynLoc.WriteLine($" {key}: \"{loc["simp_chinese"]}\"");
			spanishDynLoc.WriteLine($" {key}: \"{loc["spanish"]}\"");
		}
	}
}
