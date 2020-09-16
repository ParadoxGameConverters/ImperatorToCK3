#include "outTitles.h"
#include <filesystem>
#include <fstream>


void CK3::outputTitles(const std::string& outputModName, const std::map<std::string, std::shared_ptr<Title>>& titles)
{
	unsigned char bom[] = { 0xEF,0xBB,0xBF };

	for (const auto& [first, second] : titles)
	{
		std::ofstream output("output/" + outputModName + "/common/landed_titles/" + first + ".txt"); // dumping all into one file
		if (!output.is_open())
			throw std::runtime_error(
				"Could not create landed titles file: output/" + outputModName + "/common/landed_titles/" + first + ".txt");
		output.write(reinterpret_cast<char*>(bom), sizeof(bom)); // to make the file utf8-bom
		output << *second;
		output.close();
	}
}