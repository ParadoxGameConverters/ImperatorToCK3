#include "ProvinceName.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



Imperator::ProvinceName::ProvinceName(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void Imperator::ProvinceName::registerKeys() {
	registerKeyword("name", [this](std::istream& theStream) {
		name = commonItems::getString(theStream);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}