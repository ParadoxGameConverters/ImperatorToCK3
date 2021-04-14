#include "outCoas.h"
#include "CK3/Titles/Title.h"
#include <filesystem>
#include <fstream>



using std::string;
using std::shared_ptr;
using std::map;
using std::ofstream;
using std::runtime_error;


void CK3::outputCoas(const string& outputModName, const map<string, shared_ptr<Title>>& titles) {
	ofstream output("output/" + outputModName + "/common/coat_of_arms/coat_of_arms/fromImperator.txt"); // dumping all into one file
	if (!output.is_open()) {
		throw runtime_error("Could not create coat of arms file: output/" + outputModName + "/common/coat_of_arms/coat_of_arms/fromImperator.txt");
	}
	
	for (const auto& [titleName, title] : titles) {
		const auto& coa = title->getCoA();
		if (coa)
			output << titleName << *coa << "\n";
	}

	output.close();
}