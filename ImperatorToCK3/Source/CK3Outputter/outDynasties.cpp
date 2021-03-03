#include "outDynasties.h"
#include "CK3/Dynasties/Dynasty.h"
#include <filesystem>
#include <fstream>
#include "CommonFunctions.h"



void CK3::outputDynasties(const std::string& outputModName, const std::map<std::string, std::shared_ptr<Dynasty>>& dynasties) {
	std::string outputPath = "output/" + outputModName + "/common/dynasties/imp_dynasties.txt";
	std::ofstream output(outputPath); // dumping all into one file
	if (!output.is_open()) {
		throw std::runtime_error("Could not create dynasties file: " + outputPath);
	}
	output << commonItems::utf8BOM;
	for (const auto& [id, dynasty] : dynasties) {
		output << *dynasty;
	}
	output.close();
}
