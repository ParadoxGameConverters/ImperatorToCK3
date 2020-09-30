#include "outCharacters.h"
#include <filesystem>
#include <fstream>
#include "../commonItems/CommonFunctions.h"


void CK3::outputCharacters(const std::string& outputModName, const std::map<std::string, std::shared_ptr<Character>>& characters)
{
	std::ofstream output("output/" + outputModName + "/history/characters/fromImperator.txt"); // dumping all into one file
	if (!output.is_open())
		throw std::runtime_error(
			"Could not create landed titles file: output/" + outputModName + "/history/characters/fromImperator.txt");
	output << commonItems::utf8BOM;
	for (const auto& [id, character] : characters)
	{
		output << *character;
	}
	output.close();
}
