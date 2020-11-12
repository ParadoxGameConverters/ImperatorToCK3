#include "County.h"
#include "ParserHelpers.h"

mappers::County::County(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::County::registerKeys()
{
	
	registerRegex("b_[A-Za-z0-9_-]+", [this](const std::string& baronyName, std::istream& theStream) {
		const auto provID = Barony(theStream).getProvinceID();
		if (provID) provinces.insert(std::pair(provID.value(), nullptr));
	});
	registerKeyword(commonItems::catchallRegex, commonItems::ignoreItem);
}


bool mappers::County::countyContainsProvince(const int province) const
{
	if (provinces.count(province))
		return true;
	return false;
}
