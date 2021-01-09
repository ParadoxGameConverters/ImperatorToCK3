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
	registerKeyword("provinces", [this](const std::string& unused, std::istream& theStream) {
		for (const auto& id : commonItems::ullongList{theStream}.getULlongs())
			provinces.insert(id);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

bool mappers::ImperatorArea::areaContainsProvince(const unsigned long long province) const
{
	return provinces.contains(province);
}
