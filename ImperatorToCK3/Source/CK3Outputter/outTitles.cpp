#include "outTitles.h"
#include <filesystem>
#include <fstream>
#include "../commonItems/OSCompatibilityLayer.h"
#include "../commonItems/CommonFunctions.h"


void CK3::outputTitleHistory(const std::shared_ptr<Title>& title, std::ofstream& outputStream)
{
	outputStream << title->getName() << " = {\n";
	if (title->holder == "0") outputStream << "\t1.1.1 = { holder = 0 }\n";
	else
	{
		outputStream << "\t867.1.1 = {\n";
		
		const auto deFactoLiege = title->getDeFactoLiege();
		if (deFactoLiege) outputStream << "\t\tliege = " << deFactoLiege->getName() << "\n";
		
		outputStream << "\t\tholder = " << title->holder << "\n";
		
		if (title->government) outputStream << "\t\tgovernment = " << *title->government << "\n";
		
		outputStream << "\t}\n";
	}
	outputStream << "}\n";
}

void CK3::outputTitlesHistory(const std::string& outputModName, const std::map<std::string, std::shared_ptr<Title>>& titles)
{
	//output title history
	std::set<std::string> alreadyOutputtedTitles;
	for (const auto& [name, title] : titles) // first output kindoms + their de jure vassals to files named after the kingdoms
	{
		if (name.starts_with("k_") && !title->getDeJureVassals().empty()) // is a de jure kingdom
		{
			std::ofstream historyOutput("output/" + outputModName + "/history/titles/replace/" + name + ".txt");
			if (!historyOutput.is_open())
				throw std::runtime_error(
					"Could not create title history file: output/" + outputModName + "/history/titles/replace/" + name + ".txt");

			// output the kingdom's history
			outputTitleHistory(title, historyOutput);
			alreadyOutputtedTitles.insert(name);

			// output the kingdom's de jure vassals' history
			for (const auto& [deJureVassalName, deJureVassal] : title->getDeJureVassalsAndBelow())
			{
				outputTitleHistory(deJureVassal, historyOutput);
				alreadyOutputtedTitles.insert(deJureVassalName);
			}

			historyOutput.close();
		}
	}

	std::ofstream historyOutput("output/" + outputModName + "/history/titles/replace/00_other_titles.txt");
	if (!historyOutput.is_open())
		throw std::runtime_error("Could not create title history file: output/" + outputModName + "/history/titles/replace/00_other_titles.txt");
	for (const auto& [name, title] : titles) // output the remaining titles
	{
		if (!alreadyOutputtedTitles.contains(name))
		{
			outputTitleHistory(title, historyOutput);
			alreadyOutputtedTitles.insert(name);
		}
	}
	historyOutput.close();
}


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
		if (title->imperatorCountry && title->imperatorCountry->getCountryType() != Imperator::countryTypeEnum::real) // we don't need pirates, barbarians etc.
			continue;
		
		if (title->generated) // title is not from CK3
		{
			std::ofstream output("output/" + outputModName + "/common/landed_titles/" + name + ".txt");
			if (!output.is_open())
				throw std::runtime_error(
					"Could not create landed titles file: output/" + outputModName + "/common/landed_titles/" + name + ".txt");
			output << commonItems::utf8BOM;
			output << *title;
			output.close();
		}
	}

	outputTitlesHistory(outputModName, titles);
}

