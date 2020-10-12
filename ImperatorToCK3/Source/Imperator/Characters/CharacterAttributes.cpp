#include "CharacterAttributes.h"
#include "ParserHelpers.h"

Imperator::CharacterAttributes::CharacterAttributes(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::CharacterAttributes::registerKeys()
{
	registerKeyword("martial", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt martialInt(theStream);
		martial = martialInt.getInt();
	});
	registerKeyword("finesse", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt finesseInt(theStream);
		finesse = finesseInt.getInt();
	});
	registerKeyword("charisma", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt charismaInt(theStream);
		charisma = charismaInt.getInt();
	});
	registerKeyword("zeal", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt zealInt(theStream);
		zeal = zealInt.getInt();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}