#include "CK3WorldOutputter.h"
#include <filesystem>
#include <fstream>
#include <string>
#include "OSCompatibilityLayer.h"
#include "outCoas.h"
#include "outLocalization.h"
#include "outProvinces.h"
#include "outTitles.h"
#include "outColoredEmblems.h"


namespace CK3
{

void outputModFile(const std::string& outputName);
void createModFolder(const std::string& outputName);
void createFolders(const std::string& outputName);

}


void CK3::outputWorld(const World& CK3World, const Configuration& theConfiguration)
{
	const auto& outputName = CK3World.getOutputModName();
	createModFolder(outputName);
	outputModFile(outputName);

	LOG(LogLevel::Info) << "<- Creating folders";
	createFolders(outputName);
	
	LOG(LogLevel::Info) << "<- Writing Provinces";
	outputHistoryProvinces(outputName, CK3World.getProvinces());

	LOG(LogLevel::Info) << "<- Writing Landed Titles";
	outputTitles(outputName, CK3World.getTitles());

	LOG(LogLevel::Info) << "<- Writing Localization";
	outputLocalization(outputName, CK3World);

	LOG(LogLevel::Info) << "<- Copying named colors";
	Utils::TryCopyFile(theConfiguration.getImperatorPath()+"/game/common/named_colors/default_colors.txt", "output/" + CK3World.getOutputModName() + "/common/named_colors/imp_colors.txt");

	LOG(LogLevel::Info) << "<- Copying Coats of Arms";
	outputColoredEmblems(theConfiguration, CK3World);
	outputCoas(outputName, CK3World);
	Utils::CopyFolder(theConfiguration.getImperatorPath() + "/game/gfx/coat_of_arms/patterns", "output/" + CK3World.getOutputModName() + "/gfx/coat_of_arms/patterns");
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

void CK3::createFolders(const std::string& outputName)
{
	Utils::TryCreateFolder("output/" + outputName + "/history");
	Utils::TryCreateFolder("output/" + outputName + "/history/provinces");
	Utils::TryCreateFolder("output/" + outputName + "/history/province_mapping");
	Utils::TryCreateFolder("output/" + outputName + "/common");
	Utils::TryCreateFolder("output/" + outputName + "/common/landed_titles");
	Utils::TryCreateFolder("output/" + outputName + "/common/named_colors");
	Utils::TryCreateFolder("output/" + outputName + "/common/coat_of_arms");
	Utils::TryCreateFolder("output/" + outputName + "/common/coat_of_arms/coat_of_arms");
	Utils::TryCreateFolder("output/" + outputName + "/localization");
	Utils::TryCreateFolder("output/" + outputName + "/localization/replace");
	Utils::TryCreateFolder("output/" + outputName + "/gfx");
	Utils::TryCreateFolder("output/" + outputName + "/gfx/coat_of_arms");
	Utils::TryCreateFolder("output/" + outputName + "/gfx/coat_of_arms/colored_emblems");
	Utils::TryCreateFolder("output/" + outputName + "/gfx/coat_of_arms/patterns");

}