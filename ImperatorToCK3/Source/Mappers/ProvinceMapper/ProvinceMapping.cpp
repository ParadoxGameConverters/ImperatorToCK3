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
		const commonItems::singleULlong provinceLLong(theStream);
		ck3Provinces.push_back(provinceLLong.getULlong());
	});
	registerKeyword("imp", [this](std::istream& theStream) {
		const commonItems::singleULlong provinceLLong(theStream);
		impProvinces.push_back(provinceLLong.getULlong());
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
