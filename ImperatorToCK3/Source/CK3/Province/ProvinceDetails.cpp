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
	registerSetter("culture", culture);
	registerSetter("religion", religion);
	registerSetter("holding", holding);
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}
