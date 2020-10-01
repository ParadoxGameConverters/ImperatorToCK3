#include "outTitles.h"
#include <filesystem>
#include <fstream>
#include "../commonItems/OSCompatibilityLayer.h"
#include "../commonItems/CommonFunctions.h"


void CK3::outputTitles(const std::string& outputModName, const std::string& ck3Path, const std::map<std::string, std::shared_ptr<Title>>& titles)
{
	// blank all title history files from vanilla
	auto fileNames = commonItems::GetAllFilesInFolderRecursive(ck3Path + "/game/history/titles/");
	for (const auto& fileName : fileNames)
	{
		std::ofstream file("output/" + outputModName + "/history/titles/" + fileName);
		file.close();
	}
	
	for (const auto& [name, title] : titles)
	{
		if (title->generated) // title is not a county
		{
			std::ofstream output("output/" + outputModName + "/common/landed_titles/" + name + ".txt");
			if (!output.is_open())
				throw std::runtime_error(
					"Could not create landed titles file: output/" + outputModName + "/common/landed_titles/" + name + ".txt");
			output << commonItems::utf8BOM;
			output << *title;
			output.close();
		}
		

		//output title history
		std::ofstream historyOutput("output/" + outputModName + "/history/titles/" + name + ".txt");
		if (!historyOutput.is_open())
			throw std::runtime_error(
				"Could not create title history file: output/" + outputModName + "/history/titles/" + name + ".txt");
		historyOutput << name << " = {\n";

		if (title->holder == "0") historyOutput << "\t" << title->historyString << "\n";
		else historyOutput << "\t867.1.1 = { holder = " << title->holder << " }\n";
		
		historyOutput << "}\n";
		historyOutput.close();
	}
}
