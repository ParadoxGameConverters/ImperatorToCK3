using System.Collections.Generic;
using ImperatorToCK3.CK3;
using commonItems;
using System.IO;

namespace ImperatorToCK3.Outputter {
	public static class LocalizationOutputter {
		public static void OutputLocalization(string imperatorPath, string outputName, World ck3World, IMPERATOR_DE_JURE deJure) {
			// copy character/family names localization
			SystemUtils.TryCopyFile(imperatorPath + "/game/localization/english/character_names_l_english.yml",
				"output/" + outputName + "/localization/replace/english/IMPERATOR_character_names_l_english.yml");
			SystemUtils.TryCopyFile(imperatorPath + "/game/localization/french/character_names_l_french.yml",
				"output/" + outputName + "/localization/replace/french/IMPERATOR_character_names_l_french.yml");
			SystemUtils.TryCopyFile(imperatorPath + "/game/localization/german/character_names_l_german.yml",
				"output/" + outputName + "/localization/replace/german/IMPERATOR_character_names_l_german.yml");
			SystemUtils.TryCopyFile(imperatorPath + "/game/localization/russian/character_names_l_russian.yml",
				"output/" + outputName + "/localization/replace/russian/IMPERATOR_character_names_l_russian.yml");
			SystemUtils.TryCopyFile(imperatorPath + "/game/localization/simp_chinese/character_names_l_simp_chinese.yml",
				"output/" + outputName + "/localization/replace/simp_chinese/IMPERATOR_character_names_l_simp_chinese.yml");
			SystemUtils.TryCopyFile(imperatorPath + "/game/localization/spanish/character_names_l_spanish.yml",
				"output/" + outputName + "/localization/replace/spanish/IMPERATOR_character_names_l_spanish.yml");

			using var english = new StreamWriter("output/" + outputName + "/localization/replace/english/converter_l_english.yml");
			using var french = new StreamWriter("output/" + outputName + "/localization/replace/french/converter_l_french.yml");
			using var german = new StreamWriter("output/" + outputName + "/localization/replace/german/converter_l_german.yml");
			using var russian = new StreamWriter("output/" + outputName + "/localization/replace/russian/converter_l_russian.yml");
			using var simp_chinese = new StreamWriter("output/" + outputName + "/localization/replace/spanish/converter_l_simp_chinese.yml");
			using var spanish = new StreamWriter("output/" + outputName + "/localization/replace/spanish/converter_l_spanish.yml");

			english.WriteLine(CommonFunctions.UTF8BOM + "l_english:");
			french.WriteLine(CommonFunctions.UTF8BOM + "l_french:");
			german.WriteLine(CommonFunctions.UTF8BOM + "l_german:");
			russian.WriteLine(CommonFunctions.UTF8BOM + "l_russian:");
			simp_chinese.WriteLine(CommonFunctions.UTF8BOM + "l_simp_chinese:");
			spanish.WriteLine(CommonFunctions.UTF8BOM + "l_spanish:");

			// title localization
			foreach (var title in ck3World.LandedTitles.Values) {
				foreach (var (key, loc) in title.Localizations) {
					english.WriteLine($" {key}: \"{loc.english}\"");
					french.WriteLine($" {key}: \"{loc.french}\"");
					german.WriteLine($" {key}: \"{loc.german}\"");
					russian.WriteLine($" {key}: \"{loc.russian}\"");
					simp_chinese.WriteLine($" {key}: \"{loc.simp_chinese}\"");
					spanish.WriteLine($" {key}: \"{loc.spanish}\"");
				}
			}
			if (deJure == IMPERATOR_DE_JURE.REGIONS) {
				SystemUtils.TryCopyFolder("blankMod/optionalFiles/ImperatorDeJure/localization", "output/" + outputName + "/localization/");
			}

			// character name localization
			var uniqueKeys = new HashSet<string>();
			foreach (var character in ck3World.Characters.Values) {
				foreach (var (key, loc) in character.Localizations) {
					if (!uniqueKeys.Contains(key)) {
						english.WriteLine($" {key}: \"{loc.english}\"");
						french.WriteLine($" {key}: \"{loc.french}\"");
						german.WriteLine($" {key}: \"{loc.german}\"");
						russian.WriteLine($" {key}: \"{loc.russian}\"");
						simp_chinese.WriteLine($" {key}: \"{loc.simp_chinese}\"");
						spanish.WriteLine($" {key}: \"{loc.spanish}\"");

						uniqueKeys.Add(key);
					}
				}
			}

			// dynasty localization
			using var englishDynLoc = new StreamWriter("output/" + outputName + "/localization/replace/english/imp_dynasty_l_english.yml");
			using var frenchDynLoc = new StreamWriter("output/" + outputName + "/localization/replace/french/imp_dynasty_l_french.yml");
			using var germanDynLoc = new StreamWriter("output/" + outputName + "/localization/replace/german/imp_dynasty_l_german.yml");
			using var russianDynLoc = new StreamWriter("output/" + outputName + "/localization/replace/russian/imp_dynasty_l_russian.yml");
			using var simp_chineseDynLoc = new StreamWriter("output/" + outputName + "/localization/replace/simp_chinese/imp_dynasty_l_simp_chinese.yml");
			using var spanishDynLoc = new StreamWriter("output/" + outputName + "/localization/replace/spanish/imp_dynasty_l_spanish.yml");

			englishDynLoc.WriteLine(CommonFunctions.UTF8BOM + "l_english:");
			frenchDynLoc.WriteLine(CommonFunctions.UTF8BOM + "l_french:");
			germanDynLoc.WriteLine(CommonFunctions.UTF8BOM + "l_german:");
			russianDynLoc.WriteLine(CommonFunctions.UTF8BOM + "l_russian:");
			simp_chineseDynLoc.WriteLine(CommonFunctions.UTF8BOM + "l_simp_chinese:");
			spanishDynLoc.WriteLine(CommonFunctions.UTF8BOM + "l_spanish:");

			foreach (var dynasty in ck3World.Dynasties.Values) {
				var (key, loc) = dynasty.Localization;
				englishDynLoc.WriteLine($" {key}: \"{loc.english}\"");
				frenchDynLoc.WriteLine($" {key}: \"{loc.french}\"");
				germanDynLoc.WriteLine($" {key}: \"{loc.german}\"");
				russianDynLoc.WriteLine($" {key}: \"{loc.russian}\"");
				simp_chineseDynLoc.WriteLine($" {key}: \"{loc.simp_chinese}\"");
				spanishDynLoc.WriteLine($" {key}: \"{loc.spanish}\"");
			}
		}
	}
}
