#include "ProvinceMapping.h"
#include "ParserHelpers.h"

mappers::ProvinceMapping::ProvinceMapping(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::ProvinceMapping::registerKeys()
{
	registerKeyword("ck3", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt provinceInt(theStream);
		ck3Provinces.push_back(provinceInt.getInt());
	});
	registerKeyword("imp", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt provinceInt(theStream);
		impProvinces.push_back(provinceInt.getInt());
	});
	registerRegex("[a-zA-Z0-9\\_.:-]+", commonItems::ignoreItem);
}
