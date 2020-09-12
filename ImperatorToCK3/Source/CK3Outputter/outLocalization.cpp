#include "outLocalization.h"
#include <filesystem>
#include <fstream>

void CK3::outputLocalization(const std::string& outputName, const World& CK3World)
{
	std::ofstream english("output/" + outputName + "/localization/english/converter_l_english.yml");
	//std::ofstream french("output/" + outputName + "/localization/french/converter_l_french.yml");
	//std::ofstream german("output/" + outputName + "/localization/german/converter_l_german.yml");
	//std::ofstream russian("output/" + outputName + "/localization/russian/converter_l_russian.yml");
	//std::ofstream spanish("output/" + outputName + "/localization/spanish/converter_l_spanish.yml");
	if (!english.is_open())
		throw std::runtime_error("Error writing localization file! Is the output folder writable?");
	/*if (!french.is_open())
		throw std::runtime_error("Error writing localization file! Is the output folder writable?");
	if (!german.is_open())
		throw std::runtime_error("Error writing localization file! Is the output folder writable?");
	if (!russian.is_open())
		throw std::runtime_error("Error writing localization file! Is the output folder writable?");
	if (!spanish.is_open())
		throw std::runtime_error("Error writing localization file! Is the output folder writable?");*/
	
	english << "\xEF\xBB\xBFl_english:\n"; // write BOM
	/*french << "\xEF\xBB\xBFl_french:\n";	// write BOM
	german << "\xEF\xBB\xBFl_german:\n";	// write BOM
	russian << "\xEF\xBB\xBFl_russian:\n"; // write BOM
	spanish << "\xEF\xBB\xBFl_spanish:\n"; // write BOM*/

	for (const auto& [first, second] : CK3World.getTitles())
	{
		if (second->getEnglishLoc()) english << " " << first << ": \"" << second->getEnglishLoc().value() << "\"\n";
		//french << " " << first << ": \"" << second->getFrenchLoc().value() << "\"\n";
		//german << " " << first << ": \"" << second->getGermanLoc().value() << "\"\n";
		//russian << " " << first << ": \"" << second->getRussianLoc().value() << "\"\n";
		//spanish << " " << first << ": \"" << second->getSpanishLoc().value() << "\"\n";
	}
	english.close();
	//french.close();
	//german.close();
	//russian.close();
	//spanish.close();
}