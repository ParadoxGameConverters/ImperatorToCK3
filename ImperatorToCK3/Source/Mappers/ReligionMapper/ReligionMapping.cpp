#include "ReligionMapping.h"
#include "ParserHelpers.h"

mappers::ReligionMapping::ReligionMapping(std::istream& theStream)
{
	registerKeyword("ck3", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString religionString(theStream);
		ck3Religion = religionString.getString();
	});
	registerKeyword("imp", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString religionString(theStream);
		impReligions.insert(religionString.getString());
	});
	registerKeyword("region", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString regionString(theStream);
		ck3Regions.insert(regionString.getString());
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}