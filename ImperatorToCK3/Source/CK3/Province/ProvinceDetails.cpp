#include "ProvinceDetails.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include "ParserHelpers.h"

CK3::ProvinceDetails::ProvinceDetails(const std::string& filePath)
{
	registerKeys();
	if (Utils::DoesFileExist(filePath))
	{
		parseFile(filePath);
	}
	clearRegisteredKeywords();
}

void CK3::ProvinceDetails::updateWith(const std::string& filePath)
{
	registerKeys();
	if (Utils::DoesFileExist(filePath))
	{
		parseFile(filePath);
	}
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
	/*registerKeyword("owner", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString ownerStr(theStream);
		owner = ownerStr.getString();
	});
	registerKeyword("controller", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString contStr(theStream);
		controller = contStr.getString();
	});*/
	registerKeyword("culture", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString cultureStr(theStream);
		culture = cultureStr.getString();
	});
	registerKeyword("religion", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString religStr(theStream);
		religion = religStr.getString();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
