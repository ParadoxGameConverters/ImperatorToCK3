#include "Duchy.h"
#include "ParserHelpers.h"

mappers::Duchy::Duchy(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::Duchy::registerKeys()
{
	registerRegex("c_[A-Za-z0-9_-]+", [this](const std::string& countyName, std::istream& theStream) {
		counties.insert(std::pair(countyName, County(theStream)));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

bool mappers::Duchy::duchyContainsProvince(int province) const
{
	for (const auto& county: counties)
		if (county.second.countyContainsProvince(province))
			return true;
	return false;
}
