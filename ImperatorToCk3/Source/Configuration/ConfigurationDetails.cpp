#include "ConfigurationDetails.h"
#include "Log.h"
#include "ParserHelpers.h"

ConfigurationDetails::ConfigurationDetails(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ConfigurationDetails::registerKeys()
{
	registerKeyword("ImperatorSavePath", [this](const std::string& unused, std::istream& theStream) {
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
		const commonItems::singleInt deJureInt(theStream);
		imperatorDeJure = IMPERATOR_DE_JURE(deJureInt.getInt());
		Log(LogLevel::Info) << "CK3 de iure set to: " << deJureInt.getInt();
	});
	registerRegex("[a-zA-Z0-9\\_.:]+", commonItems::ignoreItem);
}