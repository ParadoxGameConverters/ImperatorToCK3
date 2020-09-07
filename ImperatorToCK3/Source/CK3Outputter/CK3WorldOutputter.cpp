#include "CK3WorldOutputter.h"
#include <filesystem>
#include <fstream>
#include <string>
#include "OSCompatibilityLayer.h"


namespace CK3
{

void outputModFile(const std::string& outputName);
void createModFolder(const std::string& outputName);

}


void CK3::outputWorld(const CK3::World& CK3World)
{
	const auto& outputName = CK3World.getOutputModName();
	createModFolder(outputName);
	outputModFile(outputName);

	Utils::TryCreateFolder("output/" + CK3World.getOutputModName() + "/history/");
	Utils::TryCreateFolder("output/" + CK3World.getOutputModName() + "/history/provinces");
	Utils::TryCreateFolder("output/" + CK3World.getOutputModName() + "/history/province_mapping");

	LOG(LogLevel::Info) << "<- Writing Provinces";
	outputHistoryProvinces(CK3World);
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

void CK3::outputHistoryProvinces(const CK3::World& CK3World)
{
	std::ofstream output("output/" + CK3World.getOutputModName() + "/history/provinces/province_history.txt"); // dumping all into one file
	if (!output.is_open())
		throw std::runtime_error(
			"Could not create province history file: output/" + CK3World.getOutputModName() + "/history/provinces/province_history.txt");
	output << "# " << CK3World.getProvinces().size() << "\n";
	for (const auto& province : CK3World.getProvinces())
	{
		output << *province.second;
	}
	output.close();

	//create province mapping dummy
	std::ofstream dummy("output/" + CK3World.getOutputModName() + "/history/province_mapping/dummy.txt");
	if (!dummy.is_open())
		throw std::runtime_error(
			"Could not create province mapping file: output/" + CK3World.getOutputModName() + "/history/province_mapping/dummy.txt");
	dummy.close();
}