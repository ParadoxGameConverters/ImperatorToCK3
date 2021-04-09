#include "DeathReasonMapping.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



mappers::DeathReasonMapping::DeathReasonMapping(std::istream& theStream) {
	registerKeyword("ck3", [this](std::istream& theStream) {
		ck3Reason = commonItems::getString(theStream);
	});
	registerKeyword("imp", [this](std::istream& theStream) {
		impReasons.emplace(commonItems::getString(theStream));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}
