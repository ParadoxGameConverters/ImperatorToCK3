#include "GovernmentMapping.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

mappers::GovernmentMapping::GovernmentMapping(std::istream& theStream)
{
	registerKeyword("ck3", [this](std::istream& theStream) {
		ck3Government = commonItems::singleString(theStream).getString();
	});
	registerKeyword("imp", [this](std::istream& theStream) {
		impGovernments.insert(commonItems::singleString(theStream).getString());
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}