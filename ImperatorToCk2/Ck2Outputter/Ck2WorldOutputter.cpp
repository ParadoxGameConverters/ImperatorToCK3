#include "Ck2WorldOutputter.h"
#include <filesystem>
#include <fstream>
#include <string>



namespace Ck2WorldOutputter
{

void outputModFile(const std::string& outputName);
void createModFolder(const std::string& outputName);

}

void Ck2WorldOutputter::outputWorld(const Ck2Interface::World& world)
{
	std::ofstream output("output.txt");
	output << world.getMessage();
	output.close();

	std::string outputName{ "CK2tester" };
	outputModFile(outputName);
	createModFolder(outputName);
}


void Ck2WorldOutputter::outputModFile(const std::string& outputName)
{
	std::ofstream modFile("output/" + outputName + ".mod");
	modFile << "name = \"Converted - " << outputName << "\"\n";
	modFile << "path = \"mod/" << outputName << "\"";
	modFile.close();
}


void Ck2WorldOutputter::createModFolder(const std::string& outputName)
{
	std::filesystem::path modPath{ "output/" + outputName };
	std::filesystem::create_directory(modPath);
}