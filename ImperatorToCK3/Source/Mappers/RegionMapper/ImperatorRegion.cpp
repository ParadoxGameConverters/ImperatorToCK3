#include "ImperatorRegion.h"
#include "ParserHelpers.h"
#include "Log.h"

mappers::ImperatorRegion::ImperatorRegion(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::ImperatorRegion::registerKeys()
{
	registerKeyword("areas", [this](const std::string& unused, std::istream& theStream) {
		for (const auto& name : commonItems::stringList{ theStream }.getStrings())
			areas.emplace(name, nullptr);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

bool mappers::ImperatorRegion::regionContainsProvince(const unsigned long long province) const
{
	for (const auto& [areaName, area] : areas)
		if (area && area->areaContainsProvince(province))
			return true;
		
	return false;
}
