#include "ProvinceMapping.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

mappers::ProvinceMapping::ProvinceMapping(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::ProvinceMapping::registerKeys()
{
	registerKeyword("ck3", [this](std::istream& theStream) {
		ck3Provinces.push_back(commonItems::getULlong(theStream));
	});
	registerKeyword("imp", [this](std::istream& theStream) {
		impProvinces.push_back(commonItems::getULlong(theStream));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
