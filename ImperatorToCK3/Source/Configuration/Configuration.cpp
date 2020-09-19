#include "Configuration.h"
#include "Color.h"
#include "CommonFunctions.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include "ParserHelpers.h"

auto laFabricaDeColor = commonItems::Color::Factory();

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
		CK3Path = path.getString();
		});
	registerKeyword("CK3ModsDirectory", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString path(theStream);
		CK3ModsPath = path.getString();
		});	
	registerKeyword("output_name", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString nameStr(theStream);
		outputName = nameStr.getString();
		Log(LogLevel::Info) << "Output name set to: " << outputName;
		});
	registerKeyword("ImperatorDeJure", [this](const std::string& unused, std::istream& theStream) {
		const auto deJureString = commonItems::singleString(theStream).getString();
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
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


void Configuration::verifyImperatorPath() const
{
	if (!Utils::DoesFolderExist(ImperatorPath)) throw std::runtime_error(ImperatorPath + " does not exist!");
	if (!Utils::DoesFileExist(ImperatorPath + "/binaries/imperator.exe"))
		throw std::runtime_error(ImperatorPath + " does not contain Imperator: Rome!");
	LOG(LogLevel::Info) << "\tI:R install path is " << ImperatorPath;
}

void Configuration::verifyCK3Path() const
{
	if (!Utils::DoesFolderExist(CK3Path)) throw std::runtime_error(CK3Path + " does not exist!");
	if (!Utils::DoesFileExist(CK3Path + "/binaries/ck3.exe"))
		throw std::runtime_error(CK3Path + " does not contain Crusader Kings III!");
	LOG(LogLevel::Info) << "\tCK3 install path is " << CK3Path;
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