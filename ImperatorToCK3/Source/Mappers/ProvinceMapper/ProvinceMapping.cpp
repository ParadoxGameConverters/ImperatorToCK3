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
		const commonItems::singleULlong provinceLLong(theStream);
		ck3Provinces.push_back(provinceLLong.getULlong());
	});
	registerKeyword("imp", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleULlong provinceLLong(theStream);
		impProvinces.push_back(provinceLLong.getULlong());
	});
	registerRegex("[a-zA-Z0-9\\_.:-]+", commonItems::ignoreItem);
}
