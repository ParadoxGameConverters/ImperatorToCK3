#include "CharacterAttributes.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

Imperator::CharacterAttributes::CharacterAttributes(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::CharacterAttributes::registerKeys()
{
	registerKeyword("martial", [this](std::istream& theStream) {
		martial = commonItems::getInt(theStream);
	});
	registerKeyword("finesse", [this](std::istream& theStream) {
		finesse = commonItems::getInt(theStream);
	});
	registerKeyword("charisma", [this](std::istream& theStream) {
		charisma = commonItems::getInt(theStream);
	});
	registerKeyword("zeal", [this](std::istream& theStream) {
		zeal = commonItems::getInt(theStream);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}