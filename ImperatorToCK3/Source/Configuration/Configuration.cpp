#include "Configuration.h"
#include "Color.h"
#include "CommonFunctions.h"
#include "CommonRegexes.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include "ParserHelpers.h"



auto laFabricaDeColor = commonItems::Color::Factory{};


Configuration::Configuration(const commonItems::ConverterVersion& converterVersion) {
	LOG(LogLevel::Info) << "Reading configuration file";
	registerKeys();
	parseFile("configuration.txt");
	clearRegisteredKeywords();
	setOutputName();
	verifyImperatorPath();
	verifyImperatorVersion(converterVersion);
	verifyCK3Path();
	verifyCK3Version(converterVersion);
}


Configuration::Configuration(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
	setOutputName();
}


void Configuration::registerKeys() {
	registerKeyword("SaveGame", [this](std::istream& theStream) {
		SaveGamePath = commonItems::getString(theStream);
		Log(LogLevel::Info) << "Save Game set to: " << SaveGamePath;
	});
	registerKeyword("ImperatorDirectory", [this](std::istream& theStream) { ImperatorPath = commonItems::getString(theStream); });
	registerKeyword("ImperatorModsDirectory", [this](std::istream& theStream) { ImperatorModsPath = commonItems::getString(theStream); });
	registerKeyword("CK3directory", [this](std::istream& theStream) { CK3Path = commonItems::getString(theStream); });
	registerKeyword("CK3ModsDirectory", [this](std::istream& theStream) { CK3ModsPath = commonItems::getString(theStream); });
	registerKeyword("output_name", [this](std::istream& theStream) {
		outputModName = commonItems::getString(theStream);
		Log(LogLevel::Info) << "Output name set to: " << outputModName;
	});
	registerKeyword("ImperatorDeJure", [this](std::istream& theStream) {
		const auto deJureString = commonItems::getString(theStream);
		try {
			imperatorDeJure = static_cast<IMPERATOR_DE_JURE>(stoi(deJureString));
			Log(LogLevel::Info) << "ImperatorDeJure set to: " << deJureString;
		} catch (const std::exception& e) {
			Log(LogLevel::Error) << "Undefined error, ImperatorDeJure value was: " << deJureString << "; Error message: " << e.what();
		}
	});
	registerKeyword("ConvertCharacterBirthAndDeathDates", [this](std::istream& theStream) {
		const auto valStr = commonItems::getString(theStream);
		if (valStr == "true")
			convertBirthAndDeathDates = true;
		else if (valStr == "false")
			convertBirthAndDeathDates = false;
		Log(LogLevel::Info) << "Conversion of characters' birth and death dates set to: " << convertBirthAndDeathDates;
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


void Configuration::verifyImperatorPath() const {
	if (!commonItems::DoesFolderExist(ImperatorPath))
		throw std::runtime_error(ImperatorPath + " does not exist!");
	if (!commonItems::DoesFileExist(ImperatorPath + "/binaries/imperator.exe"))
		throw std::runtime_error(ImperatorPath + " does not contain Imperator: Rome!");
	LOG(LogLevel::Info) << "\tI:R install path is " << ImperatorPath;
}


void Configuration::verifyCK3Path() const {
	if (!commonItems::DoesFolderExist(CK3Path))
		throw std::runtime_error(CK3Path + " does not exist!");
	if (!commonItems::DoesFileExist(CK3Path + "/binaries/ck3.exe"))
		throw std::runtime_error(CK3Path + " does not contain Crusader Kings III!");
	LOG(LogLevel::Info) << "\tCK3 install path is " << CK3Path;
}


void Configuration::setOutputName() {
	if (outputModName.empty()) {
		outputModName = trimPath(SaveGamePath);
	}
	outputModName = trimExtension(outputModName);
	outputModName = replaceCharacter(outputModName, '-');
	outputModName = replaceCharacter(outputModName, ' ');

	outputModName = commonItems::normalizeUTF8Path(outputModName);
	LOG(LogLevel::Info) << "Using output name " << outputModName;
}

void Configuration::verifyImperatorVersion(const commonItems::ConverterVersion& converterVersion) const {
	const auto ImpVersion = GameVersion::extractVersionFromLauncher(ImperatorPath + "/launcher/launcher-settings.json");
	if (!ImpVersion) {
		Log(LogLevel::Error) << "Imperator version could not be determined, proceeding blind!";
		return;
	}

	Log(LogLevel::Info) << "Imperator version: " << ImpVersion->toShortString();

	if (converterVersion.getMinSource() > *ImpVersion) {
		Log(LogLevel::Error) << "Imperator version is v" << ImpVersion->toShortString() << ", converter requires minimum v"
							 << converterVersion.getMinSource().toShortString() << "!";
		throw std::runtime_error("Converter vs Imperator installation mismatch!");
	}
	if (!converterVersion.getMaxSource().isLargerishThan(*ImpVersion)) {
		Log(LogLevel::Error) << "Imperator version is v" << ImpVersion->toShortString() << ", converter requires maximum v"
							 << converterVersion.getMaxSource().toShortString() << "!";
		throw std::runtime_error("Converter vs Imperator installation mismatch!");
	}
}

void Configuration::verifyCK3Version(const commonItems::ConverterVersion& converterVersion) const {
	const auto CK3Version = GameVersion::extractVersionFromLauncher(CK3Path + "/launcher/launcher-settings.json");
	if (!CK3Version) {
		Log(LogLevel::Error) << "CK3 version could not be determined, proceeding blind!";
		return;
	}

	Log(LogLevel::Info) << "CK3 version: " << CK3Version->toShortString();

	if (converterVersion.getMinTarget() > *CK3Version) {
		Log(LogLevel::Error) << "CK3 version is v" << CK3Version->toShortString() << ", converter requires minimum v"
							 << converterVersion.getMinTarget().toShortString() << "!";
		throw std::runtime_error("Converter vs CK3 installation mismatch!");
	}
	if (!converterVersion.getMaxTarget().isLargerishThan(*CK3Version)) {
		Log(LogLevel::Error) << "CK3 version is v" << CK3Version->toShortString() << ", converter requires maximum v"
							 << converterVersion.getMaxTarget().toShortString() << "!";
		throw std::runtime_error("Converter vs CK3 installation mismatch!");
	}
}
