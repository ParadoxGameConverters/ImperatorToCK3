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
		const commonItems::singleInt martialInt(theStream);
		martial = martialInt.getInt();
	});
	registerKeyword("finesse", [this](std::istream& theStream) {
		const commonItems::singleInt finesseInt(theStream);
		finesse = finesseInt.getInt();
	});
	registerKeyword("charisma", [this](std::istream& theStream) {
		const commonItems::singleInt charismaInt(theStream);
		charisma = charismaInt.getInt();
	});
	registerKeyword("zeal", [this](std::istream& theStream) {
		const commonItems::singleInt zealInt(theStream);
		zeal = zealInt.getInt();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}