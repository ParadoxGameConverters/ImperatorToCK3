#include "TraitMapping.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

mappers::TraitMapping::TraitMapping(std::istream& theStream)
{
	registerKeyword("ck3", [this](std::istream& theStream) {
		const commonItems::singleString traitString(theStream);
		ck3Trait = traitString.getString();
	});
	registerKeyword("imp", [this](std::istream& theStream) {
		const commonItems::singleString traitString(theStream);
		impTraits.insert(traitString.getString());
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}