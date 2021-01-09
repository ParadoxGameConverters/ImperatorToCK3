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
	registerKeyword("culture", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString cultureStr(theStream);
		culture = cultureStr.getString();
	});
	registerKeyword("religion", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString religionStr(theStream);
		religion = religionStr.getString();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
