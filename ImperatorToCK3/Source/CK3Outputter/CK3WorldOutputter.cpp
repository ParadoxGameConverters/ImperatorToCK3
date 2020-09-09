#include "CK3WorldOutputter.h"
#include <filesystem>
#include <fstream>
#include <string>
#include "OSCompatibilityLayer.h"
#include "outProvinces.h"


namespace CK3
{

void outputModFile(const std::string& outputName);
void createModFolder(const std::string& outputName);

}


void CK3::outputWorld(const World& CK3World)
{
	const auto& outputName = CK3World.getOutputModName();
	createModFolder(outputName);
	outputModFile(outputName);

	Utils::TryCreateFolder("output/" + CK3World.getOutputModName() + "/history/");
	Utils::TryCreateFolder("output/" + CK3World.getOutputModName() + "/history/provinces");
	Utils::TryCreateFolder("output/" + CK3World.getOutputModName() + "/history/province_mapping");

	LOG(LogLevel::Info) << "<- Writing Provinces";
	outputHistoryProvinces(outputName, CK3World.getProvinces());
}


void CK3::outputModFile(const std::string& outputName)
{
	std::ofstream modFile{ "output/" + outputName + ".mod" };
	modFile << "name = \"Converted - " << outputName << "\"\n";
	modFile << "path = \"mod/" << outputName << "\"\n";
	modFile << "replace_path = \"history/province_mapping\"\n";
	modFile << "replace_path = \"history/provinces\"\n";
	modFile.close();
}


void CK3::createModFolder(const std::string& outputName)
{
	const std::filesystem::path modPath{ "output/" + outputName };
	std::filesystem::create_directories(modPath);
}
