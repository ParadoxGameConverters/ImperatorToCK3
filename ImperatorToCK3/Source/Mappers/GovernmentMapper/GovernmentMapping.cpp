#include "GovernmentMapping.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

mappers::GovernmentMapping::GovernmentMapping(std::istream& theStream)
{
	registerKeyword("ck3", [this](std::istream& theStream) {
		ck3Government = commonItems::getString(theStream);
	});
	registerKeyword("imp", [this](std::istream& theStream) {
		impGovernments.insert(commonItems::getString(theStream));
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}