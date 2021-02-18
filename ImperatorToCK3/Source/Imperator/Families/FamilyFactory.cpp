#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "FamilyFactory.h"



Imperator::Family::Factory::Factory()
{
	registerSetter("key", family->key);
	registerSetter("culture", family->culture);
	registerSetter("prestige", family->prestige);
	registerSetter("prestige_ratio", family->prestigeRatio);
	registerKeyword("minor_family", [this](std::istream& theStream) {
		family->isMinor = commonItems::getString(theStream) == "yes";
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}


std::unique_ptr<Imperator::Family> Imperator::Family::Factory::getFamily(std::istream& theStream, unsigned long long theFamilyID)
{
	family = std::make_unique<Family>();
	family->ID = theFamilyID;

	parseStream(theStream);

	return std::move(family);
}