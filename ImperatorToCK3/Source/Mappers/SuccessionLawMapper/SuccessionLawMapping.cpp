#include "SuccessionLawMapping.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



mappers::SuccessionLawMapping::SuccessionLawMapping(std::istream& theStream) {
	registerKeyword("imp", [this](std::istream& theStream) {
		impLaw = commonItems::getString(theStream);
	});
	registerKeyword("ck3", [this](std::istream& theStream) {
		ck3SuccessionLaws.emplace(commonItems::getString(theStream));
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}