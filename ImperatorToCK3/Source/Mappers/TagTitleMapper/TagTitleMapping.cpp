#include "TagTitleMapping.h"
#include "ParserHelpers.h"

mappers::TagTitleMapping::TagTitleMapping(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

std::optional<std::string> mappers::TagTitleMapping::tagRankMatch(const std::string& impTag, const std::string& rank) const
{
	if (imperatorTag == impTag && ranks.contains(rank))
		return ck3Title;
	return std::nullopt;
}

void mappers::TagTitleMapping::registerKeys()
{
	registerKeyword("ck3", [this](const std::string& unused, std::istream& theStream) {
		ck3Title = commonItems::singleString{ theStream }.getString();
	});
	registerKeyword("imp", [this](const std::string& unused, std::istream& theStream) {
		imperatorTag = commonItems::singleString{ theStream }.getString();
	});
	registerKeyword("rank", [this](const std::string& unused, std::istream& theStream) {
		ranks.emplace(commonItems::singleString{ theStream }.getString());
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
