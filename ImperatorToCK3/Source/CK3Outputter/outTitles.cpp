#include "outTitles.h"
#include <filesystem>
#include <fstream>
#include "../commonItems/OSCompatibilityLayer.h"
#include "../commonItems/CommonFunctions.h"


void CK3::outputTitles(const std::string& outputModName, const std::string& ck3Path, const std::map<std::string, Title>& titles)
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
		if (title.imperatorCountry && title.imperatorCountry->getCountryType() != Imperator::countryTypeEnum::real) // we don't need pirates, barbarians etc.
			continue;
		
		if (title.generated) // title is not a county
		{
			std::ofstream output("output/" + outputModName + "/common/landed_titles/" + name + ".txt");
			if (!output.is_open())
				throw std::runtime_error(
					"Could not create landed titles file: output/" + outputModName + "/common/landed_titles/" + name + ".txt");
			output << commonItems::utf8BOM;
			output << title;
			output.close();
		}
	}

	//output title history
	std::set<std::string> outputtedTitles;
	for (const auto& [name, title] : titles) // first output kindoms+their de jure vassals to files named after the kingdoms
	{
		if (name.find("k_")==0) // is a kingdom
		{
			std::ofstream historyOutput("output/" + outputModName + "/history/titles/replace/history_" + name + ".txt");
			if (!historyOutput.is_open())
				throw std::runtime_error(
					"Could not create title history file: output/" + outputModName + "/history/titles/replace/history_" + name + ".txt");

			// output the kingdom's history
			historyOutput << name << " = {\n";
			if (title.holder == "0") historyOutput << "\t" << title.historyString << "\n";
			else
			{
				historyOutput << "\t867.1.1 = {\n";
				if (title.deFactoLiege) historyOutput << "\t\tliege = " << *title.deFactoLiege << "\n";
				historyOutput << "\t\tholder = " << title.holder << "\n";
				historyOutput << "\t}\n";
			}
			historyOutput << "}\n";
			
			outputtedTitles.insert(name);

			// TODO: output the kingdom's de jure vassals' history

			
			historyOutput.close();
		}
		
		


		
	}
}
