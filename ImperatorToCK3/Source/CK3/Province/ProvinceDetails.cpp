#include "ProvinceDetails.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

CK3::ProvinceDetails::ProvinceDetails(const std::string& filePath)
{
	registerKeys();
	if (commonItems::DoesFileExist(filePath))
	{
		parseFile(filePath);
	}
	else Log(LogLevel::Error) << "Could not open " << filePath << " to load province details.";
	clearRegisteredKeywords();
}

void CK3::ProvinceDetails::updateWith(const std::string& filePath)
{
	registerKeys();
	if (commonItems::DoesFileExist(filePath))
	{
		parseFile(filePath);
	}
	else Log(LogLevel::Error) << "Could not open " << filePath << " to update province details.";
	clearRegisteredKeywords();
}

CK3::ProvinceDetails::ProvinceDetails(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void CK3::ProvinceDetails::registerKeys()
{
	registerKeyword("culture", [this](std::istream& theStream) {
		culture = commonItems::getString(theStream);
	});
	registerKeyword("religion", [this](std::istream& theStream) {
		religion = commonItems::getString(theStream);
	});
	registerKeyword("holding", [this](std::istream& theStream) {
		holding = commonItems::getString(theStream);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
