#include "ImperatorToCK3Converter.h"
#include "Imperator/ImperatorWorld.h"
#include "Configuration/Configuration.h"
#include "CK3/CK3World.h"
#include "CK3Outputter/CK3WorldOutputter.h"
#include "Log.h"
#include "ConverterVersion.h"
#include <nlohmann/json.hpp>
#include <fstream>



void logGameVersions(const std::string& imperatorPath, const std::string& ck3Path) {
	nlohmann::json impLauncherSettings, ck3LauncherSettings;

	try {
		std::ifstream impSettingsFile(imperatorPath + "/launcher/launcher-settings.json");
		impSettingsFile >> impLauncherSettings;
		impSettingsFile.close();
		Log(LogLevel::Info) << "Imperator: Rome version: " << impLauncherSettings["version"];
	}
	catch (const std::exception& e) {
		Log(LogLevel::Error) << "Could not determine Imperator: Rome version: " << e.what();
	}

	try {
		std::ifstream ck3SettingsFile(ck3Path + "/launcher/launcher-settings.json");
		ck3SettingsFile >> ck3LauncherSettings;
		ck3SettingsFile.close();
		Log(LogLevel::Info) << "Crusader Kings III version: " << ck3LauncherSettings["version"];
	}
	catch (const std::exception& e) {
		Log(LogLevel::Error) << "Could not determine Crusader Kings III version: " << e.what();
	}
}


void convertImperatorToCK3(const commonItems::ConverterVersion& converterVersion) {
	const Configuration theConfiguration;
	
	logGameVersions(theConfiguration.getImperatorPath(), theConfiguration.getCK3Path());

	const Imperator::World impWorld(theConfiguration);
	const CK3::World ck3World(impWorld, theConfiguration, converterVersion);
	CK3::outputWorld(ck3World, theConfiguration);

	LOG(LogLevel::Info) << "* Conversion complete *";
}