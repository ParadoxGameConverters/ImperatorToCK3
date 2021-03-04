#include "AccessoryGenes.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



Imperator::AccessoryGenes::AccessoryGenes(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void Imperator::AccessoryGenes::registerKeys() {
	registerKeyword("index", [this](std::istream& theStream) {
		index = commonItems::getInt(theStream);
	});
	registerMatcher(commonItems::stringMatch, [this](const std::string& geneName, std::istream& theStream) {
		genes.emplace(geneName, AccessoryGene(theStream));
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}
