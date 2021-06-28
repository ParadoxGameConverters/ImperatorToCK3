#include "outProvinces.h"
#include "CK3/Province/CK3Province.h"
#include "CK3/Titles/Title.h"
#include "CommonFunctions.h"
#include "Log.h"
#include <filesystem>
#include <fstream>
#include <set>



using std::set;
using std::map;
using std::string;
using std::shared_ptr;
using std::runtime_error;
using std::ofstream;


void CK3::outputHistoryProvinces(const string& outputModName,
								 const map<unsigned long long, shared_ptr<Province>>& provinces,
								 const std::map<std::string, std::shared_ptr<Title>>& titles) {
	// output provinces to files named after their de jure kingdoms
	set<unsigned long long> alreadyOutputtedProvinces;
	
	for (const auto& [name, title] : titles) {
		if (title->getRank() == TitleRank::kingdom && !title->getDeJureVassals().empty()) {	 // title is a de jure kingdom
			const auto filePath = "output/" + outputModName + "/history/provinces/" + name + ".txt";
			ofstream historyOutput(filePath);
			if (!historyOutput.is_open())
				throw runtime_error("Could not create province history file: " + filePath);

			for (const auto& [id, provPtr] : provinces) {
				if (title->kingdomContainsProvince(id)) {
					historyOutput << *provPtr;
					alreadyOutputtedProvinces.emplace(id);
				}
			}

			historyOutput.close();
		}
	}

	//create province mapping file
	ofstream provinceMappingFile("output/" + outputModName + "/history/province_mapping/province_mapping.txt");
	if (!provinceMappingFile.is_open())
		throw runtime_error("Could not create province mapping file: output/" + outputModName + "/history/province_mapping/province_mapping.txt");
	provinceMappingFile << commonItems::utf8BOM;
	if (alreadyOutputtedProvinces.size() != provinces.size()) {
		for (const auto& [id, provPtr] : provinces) {
			if (!alreadyOutputtedProvinces.contains(id)) {
				const auto baseProvID = provPtr->getBaseProvinceID();
				if (!baseProvID) {
					Log(LogLevel::Warning) << "Leftover province " << id << " has no base province id!";
				} else {
					provinceMappingFile << id << " = " << *baseProvID;
					alreadyOutputtedProvinces.emplace(id);
				}
			}
		}
	}
	provinceMappingFile.close();

	if (alreadyOutputtedProvinces.size() != provinces.size()) {
		Log(LogLevel::Error) << "Not all provinces were outputted!";
	}
}
