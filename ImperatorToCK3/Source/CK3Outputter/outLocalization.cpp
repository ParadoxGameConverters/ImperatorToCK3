#include "outLocalization.h"
#include <filesystem>
#include <fstream>

void CK3::outputLocalization(const std::string& outputName, const World& CK3World)
{
	std::ofstream english("output/" + outputName + "/localization/replace/converter_l_english.yml");
	std::ofstream french("output/" + outputName + "/localization/replace/converter_l_french.yml");
	std::ofstream german("output/" + outputName + "/localization/replace/converter_l_german.yml");
	std::ofstream russian("output/" + outputName + "/localization/replace/converter_l_russian.yml");
	std::ofstream spanish("output/" + outputName + "/localization/replace/converter_l_spanish.yml");
	if (!english.is_open())
		throw std::runtime_error("Error writing english localisation file! Is the output folder writable?");
	if (!french.is_open())
		throw std::runtime_error("Error writing french localisation file! Is the output folder writable?");
	if (!german.is_open())
		throw std::runtime_error("Error writing german localisation file! Is the output folder writable?");
	if (!russian.is_open())
		throw std::runtime_error("Error writing russian localisation file! Is the output folder writable?");
	if (!spanish.is_open())
		throw std::runtime_error("Error writing spanish localisation file! Is the output folder writable?");
	english << "\xEF\xBB\xBFl_english:\n"; // write BOM
	french << "\xEF\xBB\xBFl_french:\n";	// write BOM
	german << "\xEF\xBB\xBFl_german:\n";	// write BOM
	russian << "\xEF\xBB\xBFl_russian:\n"; // write BOM
	spanish << "\xEF\xBB\xBFl_spanish:\n"; // write BOM

	for (const auto& [unused, title] : CK3World.getTitles())
	{
		for (const auto& locBlock : title->getLocalizations())
		{
			english << " " << locBlock.first << ": \"" << locBlock.second.english << "\"\n";
			french << " " << locBlock.first << ": \"" << locBlock.second.french << "\"\n";
			german << " " << locBlock.first << ": \"" << locBlock.second.german << "\"\n";
			russian << " " << locBlock.first << ": \"" << locBlock.second.russian << "\"\n";
			spanish << " " << locBlock.first << ": \"" << locBlock.second.spanish << "\"\n";
		}
	}
	english.close();
	french.close();
	german.close();
	russian.close();
	spanish.close();
}