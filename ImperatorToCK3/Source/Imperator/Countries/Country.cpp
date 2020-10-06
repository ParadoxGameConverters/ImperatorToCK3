#include "Country.h"
#include "ParserHelpers.h"
#include "CountryName.h"
#include "CountryCurrencies.h"
#include "Log.h"

ImperatorWorld::Country::Country(std::istream& theStream, int countryID): countryID(countryID)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::Country::registerKeys()
{
	registerKeyword("tag", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString tagStr(theStream);
		tag = tagStr.getString();
	});
	registerKeyword("country_name", [this](const std::string& unused, std::istream& theStream) {
		name = CountryName(theStream).getName();
	});
	registerKeyword("flag", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString flagStr(theStream);
		flag = flagStr.getString();
	});
	registerKeyword("country_type", [this](const std::string& unused, std::istream& theStream) {
		countryType =  commonItems::singleString(theStream).getString();
	});
	registerKeyword("color", [this](const std::string& unused, std::istream& theStream) {
		color1 = commonItems::Color::Factory{}.getColor(theStream);
	});
	registerKeyword("color2", [this](const std::string& unused, std::istream& theStream) {
		color2 = commonItems::Color::Factory{}.getColor(theStream);
	});
	registerKeyword("color3", [this](const std::string& unused, std::istream& theStream) {
		color3 = commonItems::Color::Factory{}.getColor(theStream);
	});
	registerKeyword("currency_data", [this](const std::string& unused, std::istream& theStream) {
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
	registerKeyword("capital", [this](const std::string& unused, std::istream& theStream) {
		auto capitalInt = commonItems::singleInt(theStream).getInt();
		if (capitalInt > 0) capital = capitalInt;
	});
	registerRegex("family", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt familyInt(theStream);
		families.insert(std::pair(familyInt.getInt(), nullptr));
	});
	registerRegex("minor_family", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt familyInt(theStream);
		families.insert(std::pair(familyInt.getInt(), nullptr));
	});
	registerKeyword("monarch", [this](const std::string& unused, std::istream& theStream) {
		monarch = commonItems::singleInt(theStream).getInt();
	});

	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
