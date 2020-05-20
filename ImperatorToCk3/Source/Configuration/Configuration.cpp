#include "Configuration.h"
#include "../Common/CommonFunctions.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include "ParserHelpers.h"

Configuration::Configuration()
{
	LOG(LogLevel::Info) << "Reading configuration file";
	registerKeys();
	parseFile("configuration.txt");
	clearRegisteredKeywords();
	setOutputName();
	verifyImperatorPath();
	///verifyCk3Path(); /// TODO #5: enable when CK3 is released
}

Configuration::Configuration(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
	setOutputName();
}

void Configuration::registerKeys()
{
	registerKeyword("SaveGame", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString path(theStream);
		SaveGamePath = path.getString();
		Log(LogLevel::Info) << "Save Game set to: " << SaveGamePath;
		});
	registerKeyword("ImperatorDirectory", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString path(theStream);
		ImperatorPath = path.getString();
		});
	registerKeyword("ImperatorModsDirectory", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString path(theStream);
		ImperatorModsPath = path.getString();
		});
	registerKeyword("CK3directory", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString path(theStream);
		Ck3Path = path.getString();
		});
	registerKeyword("output_name", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString nameStr(theStream);
		outputName = nameStr.getString();
		Log(LogLevel::Info) << "Output name set to: " << outputName;
		});
	registerKeyword("ImperatorDeJure", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString deJureString(theStream);
		imperatorDeJure = IMPERATOR_DE_JURE(stoi(deJureString.getString()));
		Log(LogLevel::Info) << "CK3 de iure set to: " << deJureString.getString();
		});
	registerRegex("[a-zA-Z0-9\\_.:]+", commonItems::ignoreItem);
}


void Configuration::verifyImperatorPath() const
{
	if (!Utils::DoesFolderExist(ImperatorPath)) throw std::runtime_error(ImperatorPath + " does not exist!");
	if (!Utils::DoesFileExist(ImperatorPath + "/binaries/imperator.exe"))
		throw std::runtime_error(ImperatorPath + " does not contain Imperator: Rome!");
	LOG(LogLevel::Info) << "\tI:R install path is " << ImperatorPath;
}

void Configuration::verifyCk3Path() const
{
	if (!Utils::DoesFolderExist(Ck3Path)) throw std::runtime_error(Ck3Path + " does not exist!");
	LOG(LogLevel::Info) << "\tCK3 install path is " << Ck3Path;
}

void Configuration::setOutputName()
{
	if (outputName.empty()) { outputName = trimPath(SaveGamePath); }
	outputName = trimExtension(outputName);
	outputName = replaceCharacter(outputName, '-');
	outputName = replaceCharacter(outputName, ' ');

	outputName = Utils::normalizeUTF8Path(outputName);
	LOG(LogLevel::Info) << "Using output name " << outputName;
}