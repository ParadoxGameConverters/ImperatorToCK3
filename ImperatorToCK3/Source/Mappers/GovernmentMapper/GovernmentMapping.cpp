#include "GovernmentMapping.h"
#include "ParserHelpers.h"

mappers::GovernmentMapping::GovernmentMapping(std::istream& theStream)
{
	registerKeyword("ck3", [this](const std::string& unused, std::istream& theStream) {
		ck3Government = commonItems::singleString(theStream).getString();
	});
	registerKeyword("imp", [this](const std::string& unused, std::istream& theStream) {
		impGovernments.insert(commonItems::singleString(theStream).getString());
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}