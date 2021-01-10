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
		const commonItems::singleString nameStr(theStream);
		name = nameStr.getString();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}