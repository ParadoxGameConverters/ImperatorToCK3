#include "outLocalization.h"
#include "CK3/CK3World.h"
#include "CK3/Titles/Title.h"
#include "OSCompatibilityLayer.h"
#include "CommonFunctions.h"
#include <filesystem>
#include <fstream>
#include <ranges>



using std::string;
using std::set;
using std::ofstream;
using std::runtime_error;
using std::ranges::views::values;
using commonItems::utf8BOM;
using commonItems::TryCopyFile;
using commonItems::CopyFolder;


void CK3::outputLocalization(const string& imperatorPath, const string& outputName, const World& CK3World, const Configuration::IMPERATOR_DE_JURE& deJure) {
	// copy character/family names localization
	TryCopyFile(imperatorPath + "/game/localization/english/character_names_l_english.yml",
		"output/" + outputName + "/localization/replace/english/IMPERATOR_character_names_l_english.yml");
	TryCopyFile(imperatorPath + "/game/localization/french/character_names_l_french.yml",
		"output/" + outputName + "/localization/replace/french/IMPERATOR_character_names_l_french.yml");
	TryCopyFile(imperatorPath + "/game/localization/german/character_names_l_german.yml",
		"output/" + outputName + "/localization/replace/german/IMPERATOR_character_names_l_german.yml");
	TryCopyFile(imperatorPath + "/game/localization/russian/character_names_l_russian.yml",
		"output/" + outputName + "/localization/replace/russian/IMPERATOR_character_names_l_russian.yml");
	TryCopyFile(imperatorPath + "/game/localization/simp_chinese/character_names_l_simp_chinese.yml",
		"output/" + outputName + "/localization/replace/simp_chinese/IMPERATOR_character_names_l_simp_chinese.yml");
	TryCopyFile(imperatorPath + "/game/localization/spanish/character_names_l_spanish.yml",
		"output/" + outputName + "/localization/replace/spanish/IMPERATOR_character_names_l_spanish.yml");

	
	ofstream english("output/" + outputName + "/localization/replace/english/converter_l_english.yml");
	ofstream french("output/" + outputName + "/localization/replace/french/converter_l_french.yml");
	ofstream german("output/" + outputName + "/localization/replace/german/converter_l_german.yml");
	ofstream russian("output/" + outputName + "/localization/replace/russian/converter_l_russian.yml");
	ofstream simp_chinese("output/" + outputName + "/localization/replace/spanish/converter_l_simp_chinese.yml");
	ofstream spanish("output/" + outputName + "/localization/replace/spanish/converter_l_spanish.yml");

	if (!english.is_open())
		throw runtime_error("Error writing english localization file! Is the output folder writable?");
	if (!french.is_open())
		throw runtime_error("Error writing french localization file! Is the output folder writable?");
	if (!german.is_open())
		throw runtime_error("Error writing german localization file! Is the output folder writable?");
	if (!russian.is_open())
		throw runtime_error("Error writing russian localization file! Is the output folder writable?");
	if (!simp_chinese.is_open())
		throw runtime_error("Error writing simp_chinese localization file! Is the output folder writable?");
	if (!spanish.is_open())
		throw runtime_error("Error writing spanish localization file! Is the output folder writable?");

	english << utf8BOM << "l_english:\n";
	french << utf8BOM << "l_french:\n";
	german << utf8BOM << "l_german:\n";
	russian << utf8BOM << "l_russian:\n";
	simp_chinese << utf8BOM << "l_simp_chinese:\n";
	spanish << utf8BOM << "l_spanish:\n";

	// title localization
	for (const auto& title : CK3World.getTitles() | values) {
		for (const auto& [key, loc] : title->getLocalizations()) {
			english << " " << key << ": \"" << loc.english << "\"\n";
			french << " " << key << ": \"" << loc.french << "\"\n";
			german << " " << key << ": \"" << loc.german << "\"\n";
			russian << " " << key << ": \"" << loc.russian << "\"\n";
			simp_chinese << " " << key << ": \"" << loc.simp_chinese << "\"\n";
			spanish << " " << key << ": \"" << loc.spanish << "\"\n";
		}
	}
	if (deJure == Configuration::IMPERATOR_DE_JURE::REGIONS) {
		CopyFolder("blankMod/optionalFiles/ImperatorDeJure/localization", "output/" + outputName + "/localization/");
	}

	// character name localization
	set<string> uniqueKeys;
	for (const auto& character : CK3World.getCharacters() | values) {
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
	ofstream englishDynLoc("output/" + outputName + "/localization/replace/english/imp_dynasty_l_english.yml");
	ofstream frenchDynLoc("output/" + outputName + "/localization/replace/french/imp_dynasty_l_french.yml");
	ofstream germanDynLoc("output/" + outputName + "/localization/replace/german/imp_dynasty_l_german.yml");
	ofstream russianDynLoc("output/" + outputName + "/localization/replace/russian/imp_dynasty_l_russian.yml");
	ofstream simp_chineseDynLoc("output/" + outputName + "/localization/replace/simp_chinese/imp_dynasty_l_simp_chinese.yml");
	ofstream spanishDynLoc("output/" + outputName + "/localization/replace/spanish/imp_dynasty_l_spanish.yml");

	if (!englishDynLoc.is_open())
		throw runtime_error("Error writing english localization file! Is the output folder writable?");
	if (!frenchDynLoc.is_open())
		throw runtime_error("Error writing french localization file! Is the output folder writable?");
	if (!germanDynLoc.is_open())
		throw runtime_error("Error writing german localization file! Is the output folder writable?");
	if (!russianDynLoc.is_open())
		throw runtime_error("Error writing russian localization file! Is the output folder writable?");
	if (!simp_chineseDynLoc.is_open())
		throw runtime_error("Error writing simp_chinese localization file! Is the output folder writable?");
	if (!spanishDynLoc.is_open())
		throw runtime_error("Error writing spanish localization file! Is the output folder writable?");

	englishDynLoc << utf8BOM << "l_english:\n";
	frenchDynLoc << utf8BOM << "l_french:\n";
	germanDynLoc << utf8BOM << "l_german:\n";
	russianDynLoc << utf8BOM << "l_russian:\n";
	simp_chineseDynLoc << utf8BOM << "l_simp_chinese:\n";
	spanishDynLoc << utf8BOM << "l_spanish:\n";

	for (const auto& dynasty : CK3World.getDynasties() | values) {
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
