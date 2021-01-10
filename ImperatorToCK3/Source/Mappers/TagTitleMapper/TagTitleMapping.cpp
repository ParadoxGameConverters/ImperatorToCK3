#include "TagTitleMapping.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

mappers::TagTitleMapping::TagTitleMapping(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

std::optional<std::string> mappers::TagTitleMapping::tagRankMatch(const std::string& impTag, const std::string& rank) const
{
	if (imperatorTag != impTag || (!ranks.empty() && !ranks.contains(rank)))
		return std::nullopt;

	return ck3Title;
}

void mappers::TagTitleMapping::registerKeys()
{
	registerKeyword("ck3", [this](const std::string& unused, std::istream& theStream) {
		ck3Title = commonItems::getString(theStream);
	});
	registerKeyword("imp", [this](const std::string& unused, std::istream& theStream) {
		imperatorTag = commonItems::getString(theStream);
	});
	registerKeyword("rank", [this](const std::string& unused, std::istream& theStream) {
		ranks.emplace(commonItems::getString(theStream));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
