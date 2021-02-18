#include "TraitMapping.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

mappers::TraitMapping::TraitMapping(std::istream& theStream)
{
	registerKeyword("ck3", [this](std::istream& theStream) {
		ck3Trait = commonItems::getString(theStream);
	});
	registerKeyword("imp", [this](std::istream& theStream) {
		impTraits.insert(commonItems::getString(theStream));
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}