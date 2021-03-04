#include "ImperatorRegion.h"
#include "ImperatorArea.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "Log.h"



mappers::ImperatorRegion::ImperatorRegion(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::ImperatorRegion::registerKeys() {
	registerKeyword("areas", [this](std::istream& theStream) {
		for (const auto& name : commonItems::getStrings(theStream))
			areas.emplace(name, nullptr);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}

bool mappers::ImperatorRegion::regionContainsProvince(const unsigned long long province) const {
	return std::ranges::any_of(areas, [&](const auto& areaItr) {
		return areaItr.second && areaItr.second->areaContainsProvince(province);
	});
}
