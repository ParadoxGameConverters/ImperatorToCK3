#include "Configuration.h"
#include "Color.h"
#include "CommonFunctions.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

auto laFabricaDeColor = commonItems::Color::Factory{};

Configuration::Configuration()
{
	LOG(LogLevel::Info) << "Reading configuration file";
	registerKeys();
	parseFile("configuration.txt");
	clearRegisteredKeywords();
	setOutputName();
	verifyImperatorPath();
	verifyCK3Path();
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
	registerKeyword("SaveGame", [this](std::istream& theStream) {
		SaveGamePath = commonItems::getString(theStream);
		Log(LogLevel::Info) << "Save Game set to: " << SaveGamePath;
	});
	registerKeyword("ImperatorDirectory", [this](std::istream& theStream) {
		ImperatorPath = commonItems::getString(theStream);
	});
	registerKeyword("ImperatorModsDirectory", [this](std::istream& theStream) {
		ImperatorModsPath = commonItems::getString(theStream);
	});
	registerKeyword("CK3directory", [this](std::istream& theStream) {
		CK3Path = commonItems::getString(theStream);
	});
	registerKeyword("CK3ModsDirectory", [this](std::istream& theStream) {
		CK3ModsPath = commonItems::getString(theStream);
	});	
	registerKeyword("output_name", [this](std::istream& theStream) {
		outputModName = commonItems::getString(theStream);
		Log(LogLevel::Info) << "Output name set to: " << outputModName;
	});
	registerKeyword("ImperatorDeJure", [this](std::istream& theStream) {
		const auto deJureString = commonItems::getString(theStream);
		try
		{
			imperatorDeJure = static_cast<IMPERATOR_DE_JURE>(stoi(deJureString));
			Log(LogLevel::Info) << "CK3 de iure set to: " << deJureString;
		}
		catch (const std::exception& e)
		{
			Log(LogLevel::Error) << "Undefined error, ImperatorDeJure value was: " << deJureString << "; Error message: " << e.what();
		}
	});
	registerKeyword("ConvertCharacterBirthAndDeathDates", [this](std::istream& theStream) {
		const auto valStr = commonItems::getString(theStream);
		if (valStr == "true") convertBirthAndDeathDates = true;
		else if (valStr == "false") convertBirthAndDeathDates = false;
		Log(LogLevel::Info) << "Conversion of characters' birth and death dates set to: " << convertBirthAndDeathDates;
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}


void Configuration::verifyImperatorPath() const
{
	if (!commonItems::DoesFolderExist(ImperatorPath)) throw std::runtime_error(ImperatorPath + " does not exist!");
	if (!commonItems::DoesFileExist(ImperatorPath + "/binaries/imperator.exe"))
		throw std::runtime_error(ImperatorPath + " does not contain Imperator: Rome!");
	LOG(LogLevel::Info) << "\tI:R install path is " << ImperatorPath;
}

void Configuration::verifyCK3Path() const
{
	if (!commonItems::DoesFolderExist(CK3Path)) throw std::runtime_error(CK3Path + " does not exist!");
	if (!commonItems::DoesFileExist(CK3Path + "/binaries/ck3.exe"))
		throw std::runtime_error(CK3Path + " does not contain Crusader Kings III!");
	LOG(LogLevel::Info) << "\tCK3 install path is " << CK3Path;
}

void Configuration::setOutputName()
{
	if (outputModName.empty()) { outputModName = trimPath(SaveGamePath); }
	outputModName = trimExtension(outputModName);
	outputModName = replaceCharacter(outputModName, '-');
	outputModName = replaceCharacter(outputModName, ' ');

	outputModName = commonItems::normalizeUTF8Path(outputModName);
	LOG(LogLevel::Info) << "Using output name " << outputModName;
}