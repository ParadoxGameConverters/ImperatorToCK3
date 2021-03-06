#include "outVersion.h"
#include "Mappers/VersionParser/VersionParser.h"
#include  <fstream>



std::ostream& mappers::operator<<(std::ostream& output, const VersionParser& versionParser) {
	// read commit id
	std::string commitID;
	std::ifstream commitIdFile{ "../commit_id.txt" };
	commitIdFile >> commitID;
	commitIdFile.close();

	output << "\n\n";
	output << "************ -= The Paradox Converters Team =- ********************\n";
	output << "* Converter build based on commit " << commitID << "\"\n";
	output << "* " << versionParser.descriptionLine << "\n";
	output << "* Built on " << __TIMESTAMP__ << "\n";
	output << "*********** + Imperator: Rome To Crusader Kings III + **************\n";
	return output;
}
