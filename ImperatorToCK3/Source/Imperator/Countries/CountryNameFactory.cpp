#include "CountryNameFactory.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



Imperator::CountryName::Factory::Factory() {
	registerKeyword("name", [this](std::istream& theStream){
		countryName->name = commonItems::getString(theStream);
	});
	registerKeyword("adjective", [this](std::istream& theStream){
		countryName->adjective = commonItems::getString(theStream);
	});
	registerKeyword("base", [&](std::istream& theStream){
		auto tempCountryName = std::move(countryName);
		tempCountryName->base = getCountryName(theStream);
		countryName = std::move(tempCountryName);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


std::unique_ptr<Imperator::CountryName> Imperator::CountryName::Factory::getCountryName(std::istream& theStream) {
	countryName = std::make_unique<CountryName>();

	parseStream(theStream);

	return std::move(countryName);
}