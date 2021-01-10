#include "ProvinceName.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

Imperator::ProvinceName::ProvinceName(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::ProvinceName::registerKeys()
{
	registerKeyword("name", [this](std::istream& theStream) {
		const commonItems::singleString nameStr(theStream);
		name = nameStr.getString();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}