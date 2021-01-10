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
		const commonItems::singleString nameStr(theStream);
		key = nameStr.getString();
		});
	registerKeyword("culture", [this](std::istream& theStream) {
		const commonItems::singleString cultureStr(theStream);
		culture = cultureStr.getString();
		});
	registerKeyword("prestige", [this](std::istream& theStream) {
		const commonItems::singleDouble prestigeDouble(theStream);
		prestige = prestigeDouble.getDouble();
		});
	registerKeyword("prestige_ratio", [this](std::istream& theStream) {
		const commonItems::singleDouble prestigeRatioDouble(theStream);
		prestigeRatio = prestigeRatioDouble.getDouble();
		});
	registerKeyword("minor_family", [this](std::istream& theStream) {
		const commonItems::singleString minorFamilyStr(theStream);
		isMinor = minorFamilyStr.getString() == "yes";
		});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}