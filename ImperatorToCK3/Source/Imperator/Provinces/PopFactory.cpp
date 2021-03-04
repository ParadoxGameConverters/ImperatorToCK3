#include "PopFactory.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



Imperator::Pop::Factory::Factory() {
	registerKeyword("type", [this](std::istream& theStream) {
		pop->type = commonItems::getString(theStream);
	});
	registerKeyword("culture", [this](std::istream& theStream) {
		pop->culture = commonItems::getString(theStream);
	});
	registerKeyword("religion", [this](std::istream& theStream) {
		pop->religion = commonItems::getString(theStream);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}


std::unique_ptr<Imperator::Pop> Imperator::Pop::Factory::getPop(const std::string& idString, std::istream& theStream) {
	pop = std::make_unique<Pop>();
	pop->ID = std::stoull(idString);

	parseStream(theStream);

	return std::move(pop);
}