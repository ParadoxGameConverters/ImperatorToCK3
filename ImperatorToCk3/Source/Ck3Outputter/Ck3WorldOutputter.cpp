#include "Ck3WorldOutputter.h"
#include <filesystem>
#include <fstream>
#include <string>



namespace Ck3World
{

void outputModFile(const std::string& outputName);
void createModFolder(const std::string& outputName);

}


void Ck3World::outputWorld(const Ck3World::World& world)
{
	std::string outputName = world.getOutputModName();
	createModFolder(outputName);
	outputModFile(outputName);
}


void Ck3World::outputModFile(const std::string& outputName)
{
	std::ofstream modFile{ "output/" + outputName + ".mod" };
	modFile << "name = \"Converted - " << outputName << "\"\n";
	modFile << "path = \"mod/" << outputName << "\"";
	modFile.close();
}


void Ck3World::createModFolder(const std::string& outputName)
{
	std::filesystem::path modPath{ "output/" + outputName };
	std::filesystem::create_directories(modPath);
}