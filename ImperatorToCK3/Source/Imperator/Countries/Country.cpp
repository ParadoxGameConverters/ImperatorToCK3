#include "Country.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "CountryName.h"
#include "CountryCurrencies.h"
#include "Log.h"

Imperator::Country::Country(std::istream& theStream, unsigned long long countryID): countryID(countryID)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::Country::registerKeys()
{
	registerKeyword("tag", [this](std::istream& theStream) {
		tag = commonItems::getString(theStream);
	});
	registerKeyword("country_name", [this](std::istream& theStream) {
		name = CountryName(theStream).getName();
	});
	registerKeyword("flag", [this](std::istream& theStream) {
		flag = commonItems::getString(theStream);
	});
	registerKeyword("country_type", [this](std::istream& theStream) {
		const auto countryTypeStr = commonItems::getString(theStream);
		if (countryTypeStr == "rebels")
			countryType = countryTypeEnum::rebels;
		else if (countryTypeStr == "pirates")
			countryType = countryTypeEnum::pirates;
		else if (countryTypeStr == "barbarians")
			countryType = countryTypeEnum::barbarians;
		else if (countryTypeStr == "mercenaries")
			countryType = countryTypeEnum::mercenaries;
		else if (countryTypeStr == "real")
			countryType = countryTypeEnum::real;
		else
		{
			Log(LogLevel::Error) << "Unrecognized country type: " << countryTypeStr << ", defaulting to real.";
			countryType = countryTypeEnum::real;
		}
	});
	registerKeyword("color", [this](std::istream& theStream) {
		color1 = commonItems::Color::Factory{}.getColor(theStream);
	});
	registerKeyword("color2", [this](std::istream& theStream) {
		color2 = commonItems::Color::Factory{}.getColor(theStream);
	});
	registerKeyword("color3", [this](std::istream& theStream) {
		color3 = commonItems::Color::Factory{}.getColor(theStream);
	});
	registerKeyword("currency_data", [this](std::istream& theStream) {
		const CountryCurrencies currenciesFromBloc(theStream);
		currencies.manpower = currenciesFromBloc.getManpower();
		currencies.gold = currenciesFromBloc.getGold();
		currencies.stability = currenciesFromBloc.getStability();
		currencies.tyranny = currenciesFromBloc.getTyranny();
		currencies.war_exhaustion = currenciesFromBloc.getWarExhaustion();
		currencies.aggressive_expansion = currenciesFromBloc.getAggressiveExpansion();
		currencies.political_influence = currenciesFromBloc.getPoliticalInfluence();
		currencies.military_experience = currenciesFromBloc.getMilitaryExperience();
	});
	registerKeyword("capital", [this](std::istream& theStream) {
		auto capitalLongLong = commonItems::getULlong(theStream);
		if (capitalLongLong > 0) capital = capitalLongLong;
	});
	registerKeyword("government_key", [this](std::istream& theStream) {
		government = commonItems::getString(theStream);
	});
	registerKeyword("family", [this](std::istream& theStream) {
		families.emplace(commonItems::getULlong(theStream), nullptr);
	});
	registerKeyword("minor_family", [this](std::istream& theStream) {
		families.emplace(commonItems::getULlong(theStream), nullptr);
	});
	registerKeyword("monarch", [this](std::istream& theStream) {
		monarch = commonItems::getULlong(theStream);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}


Imperator::countryRankEnum Imperator::Country::getCountryRank() const
{
	if (provinceCount == 0) return countryRankEnum::migrantHorde;
	if (provinceCount == 1) return countryRankEnum::cityState;
	if (provinceCount <= 24) return countryRankEnum::localPower;
	if (provinceCount <= 99) return countryRankEnum::regionalPower;
	if (provinceCount <= 499) return countryRankEnum::majorPower;
	return countryRankEnum::greatPower;
}
