#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "DatedHistoryBlock.h"



DatedHistoryBlock::DatedHistoryBlock(std::istream& theStream)
{
	registerRegex(commonItems::stringRegex, [&](const std::string& key, std::istream& theStream) {
		contents[key].emplace_back(commonItems::getString(theStream));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
	parseStream(theStream);
	clearRegisteredKeywords();
}
