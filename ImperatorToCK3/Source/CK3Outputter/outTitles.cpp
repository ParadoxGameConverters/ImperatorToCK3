#include "outTitles.h"
#include <filesystem>
#include <fstream>


void CK3::outputTitles(const std::string& outputModName, const std::map<std::string, std::shared_ptr<Title>>& titles)
{
	unsigned char bom[] = { 0xEF,0xBB,0xBF };
	
	std::ofstream output("output/" + outputModName + "/common/landed_titles/ImpToCK3_landed_titles.txt"); // dumping all into one file
	if (!output.is_open())
		throw std::runtime_error(
			"Could not create landed titles file: output/" + outputModName + "/common/landed_titles/ImpToCK3_landed_titles.txt");
	output.write(reinterpret_cast<char*>(bom), sizeof(bom)); // to make the file utf8-bom
	output << "# number of title: " << titles.size() << "\n";
	for (const auto& [first, second] : titles)
	{
		output << *second;
	}
	output.close();
}