#include "ImperatorArea.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "Log.h"

mappers::ImperatorArea::ImperatorArea(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::ImperatorArea::registerKeys()
{
	registerKeyword("provinces", [this](std::istream& theStream) {
		for (const auto& id : commonItems::getULlongs(theStream))
			provinces.insert(id);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}

bool mappers::ImperatorArea::areaContainsProvince(const unsigned long long province) const
{
	return provinces.contains(province);
}
