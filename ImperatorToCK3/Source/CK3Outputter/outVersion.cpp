#include "outVersion.h"
#include <fstream>
#include "ConverterVersion.h"
#include "Log.h"



void logConverterVersion(const commonItems::ConverterVersion& versionParser) {
	// read commit id
	std::string commitID;
	std::ifstream commitIdFile{"../commit_id.txt"};
	commitIdFile >> commitID;
	commitIdFile.close();

	Log(LogLevel::Info) << "************ -= The Paradox Converters Team =- ********************";
	Log(LogLevel::Info) << "* Converter build based on commit " << commitID;
	Log(LogLevel::Info) << "* " << versionParser.getDescription();
	Log(LogLevel::Info) << "* Built on " << __TIMESTAMP__;
	Log(LogLevel::Info) << "*********** + Imperator: Rome To Crusader Kings III + *************\n";
}
