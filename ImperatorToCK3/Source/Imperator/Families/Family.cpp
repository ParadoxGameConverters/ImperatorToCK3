#include "Family.h"
#include "Log.h"
#include "ParserHelpers.h"

ImperatorWorld::Family::Family(std::istream& theStream, int theFamilyID) : familyID(theFamilyID)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::Family::updateFamily(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::Family::registerKeys()
{
	registerKeyword("key", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString nameStr(theStream);
		key = nameStr.getString();
		});
	registerKeyword("culture", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString cultureStr(theStream);
		culture = cultureStr.getString();
		});
	registerKeyword("prestige", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleDouble prestigeDouble(theStream);
		prestige = prestigeDouble.getDouble();
		});
	registerKeyword("prestige_ratio", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleDouble prestigeRatioDouble(theStream);
		prestigeRatio = prestigeRatioDouble.getDouble();
		});
	registerRegex("[A-Za-z0-9\\:_.-]+", commonItems::ignoreItem);
}