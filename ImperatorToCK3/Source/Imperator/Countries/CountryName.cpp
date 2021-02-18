#include "CountryName.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

Imperator::CountryName::CountryName(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::CountryName::registerKeys()
{
	registerKeyword("name", [this](std::istream& theStream) {
		name = commonItems::getString(theStream);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}