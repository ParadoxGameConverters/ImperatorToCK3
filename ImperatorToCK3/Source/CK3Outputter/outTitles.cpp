#include "outTitles.h"
#include "CK3/Titles/Title.h"
#include "Imperator/Countries/Country.h"
#include "OSCompatibilityLayer.h"
#include "CommonFunctions.h"
#include <filesystem>
#include <fstream>



using std::shared_ptr;
using std::string;
using std::map;
using std::set;
using std::ofstream;


void CK3::outputTitleHistory(const shared_ptr<Title>& title, ofstream& outputStream) {
	outputStream << title->getName() << " = {\n";

	outputStream << "\t867.1.1 = {\n";
		
	const auto& deFactoLiege = title->getDeFactoLiege();
	if (deFactoLiege)
		outputStream << "\t\tliege = " << deFactoLiege->getName() << "\n";
	
	outputStream << "\t\tholder = " << title->getHolder() << "\n";
	
	const auto& govOpt = title->getGovernment();
	if (govOpt)
		outputStream << "\t\tgovernment = " << *govOpt << "\n";

	const auto& succLaws = title->getSuccessionLaws();
	if (!succLaws.empty()) {
		outputStream << "\t\tsuccession_laws = {\n";
		for (const auto& law : succLaws) {
			outputStream << "\t\t\t" << law << "\n";
		}
		outputStream << "\t\t}\n";
	}

	if (title->getRank() != TitleRank::barony) {
		const auto& developmentLevelOpt = title->getDevelopmentLevel();
		if (developmentLevelOpt)
			outputStream << "\t\tchange_development_level = " << *developmentLevelOpt << "\n";
	}

	outputStream << "\t}\n";

	outputStream << "}\n";
}


void CK3::outputTitlesHistory(const string& outputModName, const map<string, shared_ptr<Title>>& titles) {
	//output title history
	set<string> alreadyOutputtedTitles;
	for (const auto& [name, title] : titles) { // first output kindoms + their de jure vassals to files named after the kingdoms
		if (title->getRank() == TitleRank::kingdom && !title->getDeJureVassals().empty()) { // is a de jure kingdom
			ofstream historyOutput("output/" + outputModName + "/history/titles/replace/" + name + ".txt");
			if (!historyOutput.is_open())
				throw std::runtime_error("Could not create title history file: output/" + outputModName + "/history/titles/replace/" + name + ".txt");

			// output the kingdom's history
			outputTitleHistory(title, historyOutput);
			alreadyOutputtedTitles.insert(name);

			// output the kingdom's de jure vassals' history
			for (const auto& [deJureVassalName, deJureVassal] : title->getDeJureVassalsAndBelow()) {
				outputTitleHistory(deJureVassal, historyOutput);
				alreadyOutputtedTitles.insert(deJureVassalName);
			}

			historyOutput.close();
		}
	}

	ofstream historyOutput("output/" + outputModName + "/history/titles/replace/00_other_titles.txt");
	if (!historyOutput.is_open())
		throw std::runtime_error("Could not create title history file: output/" + outputModName + "/history/titles/replace/00_other_titles.txt");
	for (const auto& [name, title] : titles) { // output the remaining titles
		if (!alreadyOutputtedTitles.contains(name)) {
			outputTitleHistory(title, historyOutput);
			alreadyOutputtedTitles.insert(name);
		}
	}
	historyOutput.close();
}


void CK3::outputTitles(const string& outputModName, const string& ck3Path, const map<string, shared_ptr<Title>>& titles, const Configuration::IMPERATOR_DE_JURE& deJure) {
	// blank all title history files from vanilla
	auto fileNames = commonItems::GetAllFilesInFolderRecursive(ck3Path + "/game/history/titles/");
	for (const auto& fileName : fileNames) {
		ofstream file("output/" + outputModName + "/history/titles/" + fileName);
		file.close();
	}
	
	//output to landed_titles folder
	for (const auto& [name, title] : titles) {
		const auto& impCountry = title->getImperatorCountry();
		if (impCountry && impCountry->getCountryType() != Imperator::countryTypeEnum::real) { // we don't need pirates, barbarians etc.
			continue;
		}
		
		if (title->isImportedOrUpdatedFromImperator() && name.find("IMPTOCK3") != string::npos) {  // title is not from CK3
			ofstream output("output/" + outputModName + "/common/landed_titles/" + name + ".txt");
			if (!output.is_open()) {
				throw std::runtime_error("Could not create landed titles file: output/" + outputModName + "/common/landed_titles/" + name + ".txt");
			}
			output << commonItems::utf8BOM;
			output << *title;
			output.close();
		}
	}
	if (deJure == Configuration::IMPERATOR_DE_JURE::REGIONS) {
		commonItems::CopyFolder("blankMod/optionalFiles/ImperatorDeJure/common/landed_titles", "output/" + outputModName + "/common/landed_titles/");
	}

	outputTitlesHistory(outputModName, titles);
}
