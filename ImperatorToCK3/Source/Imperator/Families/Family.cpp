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
	registerKeyword("key", [this](std::istream& theStream) {
		const auto nameStr = commonItems::getString(theStream);
		key = nameStr;
	});
	registerKeyword("culture", [this](std::istream& theStream) {
		const auto cultureStr = commonItems::getString(theStream);
		culture = cultureStr;
	});
	registerKeyword("prestige", [this](std::istream& theStream) {
		prestige = commonItems::getDouble(theStream);
	});
	registerKeyword("prestige_ratio", [this](std::istream& theStream) {
		prestigeRatio = commonItems::getDouble(theStream);
	});
	registerKeyword("minor_family", [this](std::istream& theStream) {
		const auto minorFamilyStr = commonItems::getString(theStream);
		isMinor = minorFamilyStr == "yes";
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}