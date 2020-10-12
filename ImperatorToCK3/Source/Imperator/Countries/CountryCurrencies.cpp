#include "CountryCurrencies.h"
#include "ParserHelpers.h"

Imperator::CountryCurrencies::CountryCurrencies(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::CountryCurrencies::registerKeys()
{
	registerKeyword("manpower", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt manpowerInt(theStream);
		manpower = manpowerInt.getInt();
	});
	registerKeyword("gold", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt goldInt(theStream);
		gold = goldInt.getInt();
	});
	registerKeyword("stability", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt stabilityInt(theStream);
		stability = stabilityInt.getInt();
	});
	registerKeyword("tyranny", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt tyrannyInt(theStream);
		tyranny = tyrannyInt.getInt();
	});
	registerKeyword("war_exhaustion", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt warExhaustionInt(theStream);
		war_exhaustion = warExhaustionInt.getInt();
	});
	registerKeyword("aggressive_expansion", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt aggresiveExpansionInt(theStream);
		aggressive_expansion = aggresiveExpansionInt.getInt();
	});
	registerKeyword("political_influence", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt politicalInfluenceInt(theStream);
		political_influence = politicalInfluenceInt.getInt();
	});
	registerKeyword("military_experience", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt militaryExpInt(theStream);
		military_experience = militaryExpInt.getInt();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}