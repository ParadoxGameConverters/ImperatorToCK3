#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "CountryFactory.h"
#include "CountryCurrencies.h"
#include "CountryName.h"


Imperator::Country::Factory::Factory()
{
	registerKeyword("tag", [this](std::istream& theStream){
		country->tag = commonItems::getString(theStream);
	});
	registerKeyword("country_name", [this](std::istream& theStream) {
		country->name = CountryName(theStream).getName();
	});
	registerKeyword("flag", [this](std::istream& theStream){
		country->flag = commonItems::getString(theStream);
	});
	registerKeyword("country_type", [this](std::istream& theStream) {
		const auto countryTypeStr = commonItems::getString(theStream);
		if (countryTypeStr == "rebels")
			country->countryType = countryTypeEnum::rebels;
		else if (countryTypeStr == "pirates")
			country->countryType = countryTypeEnum::pirates;
		else if (countryTypeStr == "barbarians")
			country->countryType = countryTypeEnum::barbarians;
		else if (countryTypeStr == "mercenaries")
			country->countryType = countryTypeEnum::mercenaries;
		else if (countryTypeStr == "real")
			country->countryType = countryTypeEnum::real;
		else
		{
			Log(LogLevel::Error) << "Unrecognized country type: " << countryTypeStr << ", defaulting to real.";
			country->countryType = countryTypeEnum::real;
		}
	});
	registerKeyword("color", [this](std::istream& theStream) {
		country->color1 = commonItems::Color::Factory().getColor(theStream);
	});
	registerKeyword("color2", [this](std::istream& theStream) {
		country->color2 = commonItems::Color::Factory().getColor(theStream);
	});
	registerKeyword("color3", [this](std::istream& theStream) {
		country->color3 = commonItems::Color::Factory().getColor(theStream);
	});
	registerKeyword("currency_data", [this](std::istream& theStream) {
		const CountryCurrencies currenciesFromBloc(theStream);
		country->currencies.manpower = currenciesFromBloc.getManpower();
		country->currencies.gold = currenciesFromBloc.getGold();
		country->currencies.stability = currenciesFromBloc.getStability();
		country->currencies.tyranny = currenciesFromBloc.getTyranny();
		country->currencies.war_exhaustion = currenciesFromBloc.getWarExhaustion();
		country->currencies.aggressive_expansion = currenciesFromBloc.getAggressiveExpansion();
		country->currencies.political_influence = currenciesFromBloc.getPoliticalInfluence();
		country->currencies.military_experience = currenciesFromBloc.getMilitaryExperience();
	});
	registerKeyword("capital", [this](std::istream& theStream) {
		auto capitalProvID = commonItems::getULlong(theStream);
		if (capitalProvID > 0)
			country->capital = capitalProvID;
	});
	registerKeyword("government_key", [this](std::istream& theStream) {
		country->government = commonItems::getString(theStream);
	});
	registerKeyword("family", [this](std::istream& theStream) {
		country->families.emplace(commonItems::getULlong(theStream), nullptr);
	});
	registerKeyword("minor_family", [this](std::istream& theStream) {
		country->families.emplace(commonItems::getULlong(theStream), nullptr);
	});
	registerKeyword("monarch", [this](std::istream& theStream) {
		country->monarch = commonItems::getULlong(theStream);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}


std::unique_ptr<Imperator::Country> Imperator::Country::Factory::getCountry(std::istream& theStream, unsigned long long countryID)
{
	country = std::make_unique<Country>();
	country->ID = countryID;

	parseStream(theStream);

	return std::move(country);
}