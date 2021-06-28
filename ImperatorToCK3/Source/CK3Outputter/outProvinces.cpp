#include "outProvinces.h"
#include "CK3/Province/CK3Province.h"
#include "CK3/Titles/Title.h"
#include "CommonFunctions.h"
#include "Log.h"
#include <filesystem>
#include <fstream>
#include <ranges>
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
	set<string> alreadyOutputtedProvinces;
	for (const auto& [name, title] : titles) {
		if (title->getRank() == TitleRank::kingdom && !title->getDeJureVassals().empty()) {	 // title is a de jure kingdom
			const auto filePath = "output/" + outputModName + "/history/provinces/" + name + ".txt";
			ofstream historyOutput(filePath);
			if (!historyOutput.is_open())
				throw runtime_error("Could not create province history file: " + filePath);

			for (const auto& provincePtr : provinces | std::views::values) {
				if (title->kingdomContainsProvince(provincePtr->getID())) {
					historyOutput << *provincePtr;
					alreadyOutputtedProvinces.emplace(name);
				}
			}

			historyOutput.close();
		}
	}

	const auto numberDiff = provinces.size() - alreadyOutputtedProvinces.size();
	if (numberDiff != 0) {
		Log(LogLevel::Warning) << numberDiff << " provinces were not outputted!";
	}

	//create province mapping dummy
	ofstream dummy("output/" + outputModName + "/history/province_mapping/dummy.txt");
	if (!dummy.is_open())
		throw runtime_error(
			"Could not create province mapping file: output/" + outputModName + "/history/province_mapping/dummy.txt");
	dummy << commonItems::utf8BOM;
	dummy.close();
}
