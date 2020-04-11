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
	registerKeyword("configuration", [this](const std::string& unused, std::istream& theStream) { details = ConfigurationDetails(theStream); });
	registerRegex("[a-zA-Z0-9\\_.:]+", commonItems::ignoreItem);
}


void Configuration::verifyImperatorPath() const
{
	if (!Utils::doesFolderExist(details.ImperatorPath)) throw std::runtime_error(details.ImperatorPath + " does not exist!");
	if (!Utils::DoesFileExist(details.ImperatorPath + "/binaries/imperator.exe"))
		throw std::runtime_error(details.ImperatorPath + " does not contain Imperator: Rome!");
	LOG(LogLevel::Info) << "\tI:R install path is " << details.ImperatorPath;
}

void Configuration::verifyCk3Path() const
{
	if (!Utils::doesFolderExist(details.Ck3Path)) throw std::runtime_error(details.Ck3Path + " does not exist!");
	LOG(LogLevel::Info) << "\tCK3 install path is " << details.Ck3Path;
}

void Configuration::setOutputName()
{
	if (details.outputName.empty()) { details.outputName = trimPath(details.SaveGamePath); }
	details.outputName = trimExtension(details.outputName);
	details.outputName = replaceCharacter(details.outputName, '-');
	details.outputName = replaceCharacter(details.outputName, ' ');

	details.outputName = Utils::normalizeUTF8Path(details.outputName);
	LOG(LogLevel::Info) << "Using output name " << details.outputName;
}