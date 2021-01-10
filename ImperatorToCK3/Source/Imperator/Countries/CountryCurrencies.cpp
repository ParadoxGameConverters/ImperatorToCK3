#include "CountryCurrencies.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

Imperator::CountryCurrencies::CountryCurrencies(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::CountryCurrencies::registerKeys()
{
	registerKeyword("manpower", [this](std::istream& theStream) {
		manpower = commonItems::getInt(theStream);
	});
	registerKeyword("gold", [this](std::istream& theStream) {
		gold = commonItems::getInt(theStream);
	});
	registerKeyword("stability", [this](std::istream& theStream) {
		stability = commonItems::getInt(theStream);
	});
	registerKeyword("tyranny", [this](std::istream& theStream) {
		tyranny = commonItems::getInt(theStream);
	});
	registerKeyword("war_exhaustion", [this](std::istream& theStream) {
		war_exhaustion = commonItems::getInt(theStream);
	});
	registerKeyword("aggressive_expansion", [this](std::istream& theStream) {
		aggressive_expansion = commonItems::getInt(theStream);
	});
	registerKeyword("political_influence", [this](std::istream& theStream) {
		political_influence = commonItems::getInt(theStream);
	});
	registerKeyword("military_experience", [this](std::istream& theStream) {
		military_experience = commonItems::getInt(theStream);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}