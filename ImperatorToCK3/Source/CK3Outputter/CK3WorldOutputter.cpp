#include "CK3WorldOutputter.h"
#include <filesystem>
#include <fstream>
#include <string>



namespace CK3World
{

void outputModFile(const std::string& outputName);
void createModFolder(const std::string& outputName);

}


void CK3World::outputWorld(const CK3World::World& world)
{
	const auto outputName = world.getOutputModName();
	createModFolder(outputName);
	outputModFile(outputName);
}


void CK3World::outputModFile(const std::string& outputName)
{
	std::ofstream modFile{ "output/" + outputName + ".mod" };
	modFile << "name = \"Converted - " << outputName << "\"\n";
	modFile << "path = \"mod/" << outputName << "\"";
	modFile.close();
}


void CK3World::createModFolder(const std::string& outputName)
{
	std::filesystem::path modPath{ "output/" + outputName };
	std::filesystem::create_directories(modPath);
}