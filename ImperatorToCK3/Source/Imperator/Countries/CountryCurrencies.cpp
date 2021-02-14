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
	registerSetter("manpower", manpower);
	registerSetter("gold", gold);
	registerSetter("stability", stability);
	registerSetter("tyranny", tyranny);
	registerSetter("war_exhaustion", war_exhaustion);
	registerSetter("aggressive_expansion", aggressive_expansion);
	registerSetter("political_influence", political_influence);
	registerSetter("military_experience", military_experience);
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}