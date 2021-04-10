#include "DatedHistoryBlock.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



DatedHistoryBlock::DatedHistoryBlock(std::istream& theStream) {
	registerRegex(commonItems::stringRegex, [&](const std::string& key, std::istream& theStream) {
		contents.simpleFieldContents[key].emplace_back(commonItems::getString(theStream));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
	parseStream(theStream);
	clearRegisteredKeywords();
}
