#include "Ck2WorldOutputter.h"
#include <filesystem>
#include <fstream>
#include <string>



namespace Ck2World
{

void outputModFile(const std::string& outputName);
void createModFolder(const std::string& outputName);

}


void Ck2World::outputWorld(const Ck2World::World& world)
{
	std::string outputName{ "CK2tester" };
	createModFolder(outputName);
	outputModFile(outputName);
}


void Ck2World::outputModFile(const std::string& outputName)
{
	std::ofstream modFile{ "output/" + outputName + ".mod" };
	modFile << "name = \"Converted - " << outputName << "\"\n";
	modFile << "path = \"mod/" << outputName << "\"";
	modFile.close();
}


void Ck2World::createModFolder(const std::string& outputName)
{
	std::filesystem::path modPath{ "output/" + outputName };
	std::filesystem::create_directories(modPath);
}