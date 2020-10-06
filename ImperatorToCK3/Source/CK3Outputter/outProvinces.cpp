#include "outProvinces.h"
#include <filesystem>
#include <fstream>


void CK3::outputHistoryProvinces(const std::string& outputModName, const std::map<int, std::shared_ptr<Province>>& provinces)
{
	std::ofstream output("output/" + outputModName + "/history/provinces/province_history.txt"); // dumping all into one file
	if (!output.is_open())
		throw std::runtime_error(
			"Could not create province history file: output/" + outputModName + "/history/provinces/province_history.txt");
	output << "# number of provinces: " << provinces.size() << "\n";
	for (const auto& [first, second] : provinces)
	{
		output << *second;
	}
	output.close();

	//create province mapping dummy
	unsigned char bom[] = { 0xEF,0xBB,0xBF };
	std::ofstream dummy("output/" + outputModName + "/history/province_mapping/dummy.txt");
	dummy.write(reinterpret_cast<char*>(bom), sizeof bom); // to make the file utf8-bom
	if (!dummy.is_open())
		throw std::runtime_error(
			"Could not create province mapping file: output/" + outputModName + "/history/province_mapping/dummy.txt");
	dummy.close();
}