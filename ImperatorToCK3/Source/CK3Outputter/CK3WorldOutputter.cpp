#include "CK3WorldOutputter.h"
#include "outCoas.h"
#include "outLocalization.h"
#include "outCharacters.h"
#include "outDynasties.h"
#include "outProvinces.h"
#include "outTitles.h"
#include "outColoredEmblems.h"
#include "Configuration/Configuration.h"
#include "CK3/CK3World.h"
#include "OSCompatibilityLayer.h"
#include <filesystem>
#include <fstream>
#include <string>



namespace CK3 {

void outputModFile(const std::string& outputName);
void createModFolder(const std::string& outputName);
void createFolders(const std::string& outputName);

}


void CK3::outputWorld(const World& CK3World, const Configuration& theConfiguration) {
	LOG(LogLevel::Info) << "<- Clearing the output mod folder";
	std::filesystem::remove_all("output/" + theConfiguration.getOutputModName());
	
	const auto& outputName = theConfiguration.getOutputModName();
	createModFolder(outputName);
	outputModFile(outputName);

	LOG(LogLevel::Info) << "<- Creating folders";
	createFolders(outputName);

	LOG(LogLevel::Info) << "<- Writing Characters";
	outputCharacters(outputName, CK3World.getCharacters());

	LOG(LogLevel::Info) << "<- Writing Dynasties";
	outputDynasties(outputName, CK3World.getDynasties());
	
	LOG(LogLevel::Info) << "<- Writing Provinces";
	outputHistoryProvinces(outputName, CK3World.getProvinces(), CK3World.getTitles());

	LOG(LogLevel::Info) << "<- Writing Landed Titles";
	outputTitles(outputName, theConfiguration.getCK3Path(), CK3World.getTitles(), theConfiguration.getImperatorDeJure());

	LOG(LogLevel::Info) << "<- Writing Localization";
	outputLocalization(theConfiguration.getImperatorPath(), outputName, CK3World, theConfiguration.getImperatorDeJure());

	LOG(LogLevel::Info) << "<- Copying named colors";
	commonItems::TryCopyFile(theConfiguration.getImperatorPath()+"/game/common/named_colors/default_colors.txt",
							 "output/" + theConfiguration.getOutputModName() + "/common/named_colors/imp_colors.txt");

	LOG(LogLevel::Info) << "<- Copying Coats of Arms";
	copyColoredEmblems(theConfiguration, outputName);
	outputCoas(outputName, CK3World.getTitles());
	commonItems::CopyFolder(theConfiguration.getImperatorPath() + "/game/gfx/coat_of_arms/patterns",
							"output/" + theConfiguration.getOutputModName() + "/gfx/coat_of_arms/patterns");

	LOG(LogLevel::Info) << "<- Copying blankMod files to output";
	commonItems::CopyFolder("blankMod/output", "output/" + theConfiguration.getOutputModName());
}


void CK3::outputModFile(const std::string& outputName) {
	std::ofstream modFile{ "output/" + outputName + ".mod" };
	modFile << "name = \"Converted - " << outputName << "\"\n";
	modFile << "path = \"mod/" << outputName << "\"\n";
	modFile << "replace_path = \"history/province_mapping\"\n";
	modFile << "replace_path = \"history/provinces\"\n";
	//modFile << "replace_path = \"history/titles\"\n";
	modFile.close();
}


void CK3::createModFolder(const std::string& outputName) {
	const std::filesystem::path modPath{ "output/" + outputName };
	std::filesystem::create_directories(modPath);
}


void CK3::createFolders(const std::string& outputName) {
	commonItems::TryCreateFolder("output/" + outputName + "/history");
	commonItems::TryCreateFolder("output/" + outputName + "/history/titles");
	commonItems::TryCreateFolder("output/" + outputName + "/history/titles/replace");
	commonItems::TryCreateFolder("output/" + outputName + "/history/characters");
	commonItems::TryCreateFolder("output/" + outputName + "/history/provinces");
	commonItems::TryCreateFolder("output/" + outputName + "/history/province_mapping");
	commonItems::TryCreateFolder("output/" + outputName + "/common");
	commonItems::TryCreateFolder("output/" + outputName + "/common/coat_of_arms");
	commonItems::TryCreateFolder("output/" + outputName + "/common/coat_of_arms/coat_of_arms");
	commonItems::TryCreateFolder("output/" + outputName + "/common/dynasties");
	commonItems::TryCreateFolder("output/" + outputName + "/common/landed_titles");
	commonItems::TryCreateFolder("output/" + outputName + "/common/named_colors");
	commonItems::TryCreateFolder("output/" + outputName + "/localization");
	commonItems::TryCreateFolder("output/" + outputName + "/localization/replace");
	commonItems::TryCreateFolder("output/" + outputName + "/localization/replace/english");
	commonItems::TryCreateFolder("output/" + outputName + "/localization/replace/french");
	commonItems::TryCreateFolder("output/" + outputName + "/localization/replace/german");
	commonItems::TryCreateFolder("output/" + outputName + "/localization/replace/russian");
	commonItems::TryCreateFolder("output/" + outputName + "/localization/replace/simp_chinese");
	commonItems::TryCreateFolder("output/" + outputName + "/localization/replace/spanish");
	commonItems::TryCreateFolder("output/" + outputName + "/gfx");
	commonItems::TryCreateFolder("output/" + outputName + "/gfx/coat_of_arms");
	commonItems::TryCreateFolder("output/" + outputName + "/gfx/coat_of_arms/colored_emblems");
	commonItems::TryCreateFolder("output/" + outputName + "/gfx/coat_of_arms/patterns");
}