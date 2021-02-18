#include "Family.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

Imperator::Family::Family(std::istream& theStream, const unsigned long long theFamilyID) : familyID(theFamilyID)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::Family::updateFamily(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::Family::registerKeys()
{
	registerSetter("key", key);
	registerSetter("culture", culture);
	registerSetter("prestige", prestige);
	registerSetter("prestige_ratio", prestigeRatio);
	registerKeyword("minor_family", [this](std::istream& theStream) {
		isMinor = commonItems::getString(theStream) == "yes";
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}