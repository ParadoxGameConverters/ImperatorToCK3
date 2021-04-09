#include "outLocalization.h"
#include "CK3/CK3World.h"
#include "OSCompatibilityLayer.h"
#include "CommonFunctions.h"
#include <filesystem>
#include <fstream>
#include <ranges>



void CK3::outputLocalization(const std::string& imperatorPath, const std::string& outputName, const World& CK3World) {
	// copy character/family names localization
	commonItems::TryCopyFile(imperatorPath + "/game/localization/english/character_names_l_english.yml",
		"output/" + outputName + "/localization/replace/english/IMPERATOR_character_names_l_english.yml");
	commonItems::TryCopyFile(imperatorPath + "/game/localization/french/character_names_l_french.yml",
		"output/" + outputName + "/localization/replace/french/IMPERATOR_character_names_l_french.yml");
	commonItems::TryCopyFile(imperatorPath + "/game/localization/german/character_names_l_german.yml",
		"output/" + outputName + "/localization/replace/german/IMPERATOR_character_names_l_german.yml");
	commonItems::TryCopyFile(imperatorPath + "/game/localization/russian/character_names_l_russian.yml",
		"output/" + outputName + "/localization/replace/russian/IMPERATOR_character_names_l_russian.yml");
	commonItems::TryCopyFile(imperatorPath + "/game/localization/simp_chinese/character_names_l_simp_chinese.yml",
		"output/" + outputName + "/localization/replace/simp_chinese/IMPERATOR_character_names_l_simp_chinese.yml");
	commonItems::TryCopyFile(imperatorPath + "/game/localization/spanish/character_names_l_spanish.yml",
		"output/" + outputName + "/localization/replace/spanish/IMPERATOR_character_names_l_spanish.yml");

	
	std::ofstream english("output/" + outputName + "/localization/replace/english/converter_l_english.yml");
	std::ofstream french("output/" + outputName + "/localization/replace/french/converter_l_french.yml");
	std::ofstream german("output/" + outputName + "/localization/replace/german/converter_l_german.yml");
	std::ofstream russian("output/" + outputName + "/localization/replace/russian/converter_l_russian.yml");
	std::ofstream simp_chinese("output/" + outputName + "/localization/replace/spanish/converter_l_simp_chinese.yml");
	std::ofstream spanish("output/" + outputName + "/localization/replace/spanish/converter_l_spanish.yml");

	if (!english.is_open())
		throw std::runtime_error("Error writing english localization file! Is the output folder writable?");
	if (!french.is_open())
		throw std::runtime_error("Error writing french localization file! Is the output folder writable?");
	if (!german.is_open())
		throw std::runtime_error("Error writing german localization file! Is the output folder writable?");
	if (!russian.is_open())
		throw std::runtime_error("Error writing russian localization file! Is the output folder writable?");
	if (!simp_chinese.is_open())
		throw std::runtime_error("Error writing simp_chinese localization file! Is the output folder writable?");
	if (!spanish.is_open())
		throw std::runtime_error("Error writing spanish localization file! Is the output folder writable?");

	english << commonItems::utf8BOM << "l_english:\n";
	french << commonItems::utf8BOM << "l_french:\n";
	german << commonItems::utf8BOM << "l_german:\n";
	russian << commonItems::utf8BOM << "l_russian:\n";
	simp_chinese << commonItems::utf8BOM << "l_simp_chinese:\n";
	spanish << commonItems::utf8BOM << "l_spanish:\n";

	// title localization
	for (const auto& [_, title] : CK3World.getTitles()) {
		for (const auto& [key, loc] : title->localizations) {
			english << " " << key << ": \"" << loc.english << "\"\n";
			french << " " << key << ": \"" << loc.french << "\"\n";
			german << " " << key << ": \"" << loc.german << "\"\n";
			russian << " " << key << ": \"" << loc.russian << "\"\n";
			simp_chinese << " " << key << ": \"" << loc.simp_chinese << "\"\n";
			spanish << " " << key << ": \"" << loc.spanish << "\"\n";
		}
	}
	// character name localization
	std::set<std::string> uniqueKeys;
	for (const auto& [unused, character] : CK3World.getCharacters()) {
		for (const auto& [key, loc] : character->localizations) {
			if (!uniqueKeys.contains(key)) {
				english << " " << key << ": \"" << loc.english << "\"\n";
				french << " " << key << ": \"" << loc.french << "\"\n";
				german << " " << key << ": \"" << loc.german << "\"\n";
				russian << " " << key << ": \"" << loc.russian << "\"\n";
				simp_chinese << " " << key << ": \"" << loc.simp_chinese << "\"\n";
				spanish << " " << key << ": \"" << loc.spanish << "\"\n";

				uniqueKeys.emplace(key);
			}
		}
	}

	english.close();
	french.close();
	german.close();
	russian.close();
	simp_chinese.close();
	spanish.close();


	// dynasty localization
	std::ofstream englishDynLoc("output/" + outputName + "/localization/replace/english/imp_dynasty_l_english.yml");
	std::ofstream frenchDynLoc("output/" + outputName + "/localization/replace/french/imp_dynasty_l_french.yml");
	std::ofstream germanDynLoc("output/" + outputName + "/localization/replace/german/imp_dynasty_l_german.yml");
	std::ofstream russianDynLoc("output/" + outputName + "/localization/replace/russian/imp_dynasty_l_russian.yml");
	std::ofstream simp_chineseDynLoc("output/" + outputName + "/localization/replace/simp_chinese/imp_dynasty_l_simp_chinese.yml");
	std::ofstream spanishDynLoc("output/" + outputName + "/localization/replace/spanish/imp_dynasty_l_spanish.yml");

	if (!englishDynLoc.is_open())
		throw std::runtime_error("Error writing english localization file! Is the output folder writable?");
	if (!frenchDynLoc.is_open())
		throw std::runtime_error("Error writing french localization file! Is the output folder writable?");
	if (!germanDynLoc.is_open())
		throw std::runtime_error("Error writing german localization file! Is the output folder writable?");
	if (!russianDynLoc.is_open())
		throw std::runtime_error("Error writing russian localization file! Is the output folder writable?");
	if (!simp_chineseDynLoc.is_open())
		throw std::runtime_error("Error writing simp_chinese localization file! Is the output folder writable?");
	if (!spanishDynLoc.is_open())
		throw std::runtime_error("Error writing spanish localization file! Is the output folder writable?");

	englishDynLoc << commonItems::utf8BOM << "l_english:\n";
	frenchDynLoc << commonItems::utf8BOM << "l_french:\n";
	germanDynLoc << commonItems::utf8BOM << "l_german:\n";
	russianDynLoc << commonItems::utf8BOM << "l_russian:\n";
	simp_chineseDynLoc << commonItems::utf8BOM << "l_simp_chinese:\n";
	spanishDynLoc << commonItems::utf8BOM << "l_spanish:\n";

	for (const auto& dynasty : CK3World.getDynasties() | std::ranges::views::values) {
		const auto& [key, loc] = dynasty->getLocalization();
		englishDynLoc << " " << key << ": \"" << loc.english << "\"\n";
		frenchDynLoc << " " << key << ": \"" << loc.french << "\"\n";
		germanDynLoc << " " << key << ": \"" << loc.german << "\"\n";
		russianDynLoc << " " << key << ": \"" << loc.russian << "\"\n";
		simp_chineseDynLoc << " " << key << ": \"" << loc.simp_chinese << "\"\n";
		spanishDynLoc << " " << key << ": \"" << loc.spanish << "\"\n";
	}
	englishDynLoc.close();
	frenchDynLoc.close();
	germanDynLoc.close();
	russianDynLoc.close();
	simp_chineseDynLoc.close();
	spanishDynLoc.close();
}
