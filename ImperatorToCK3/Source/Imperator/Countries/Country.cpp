#include "Country.h"
#include "ParserHelpers.h"
#include "CountryName.h"
#include "CountryCurrencies.h"
#include "Log.h"
#include "newColor.h"

ImperatorWorld::Country::Country(std::istream& theStream, int cntrID): countryID(cntrID)
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
	registerKeyword("color", [this](const std::string& unused, std::istream& theStream) {
		const auto color1 = commonItems::newColor::Factory::getColor(theStream);
		LOG(LogLevel::Info) << " Color1 " << " of " << getTag() << " is " << color1;
	});
	registerKeyword("color2", [this](const std::string& unused, std::istream& theStream) {
		const auto color2 = commonItems::newColor::Factory::getColor(theStream);
		LOG(LogLevel::Info) << " Color2 " << " of " << getTag() << " is " << color2;
	});
	registerKeyword("color3", [this](const std::string& unused, std::istream& theStream) {
		const auto color3 = commonItems::newColor::Factory::getColor(theStream);
		LOG(LogLevel::Info) << " Color3 " << " of " << getTag() << " is " << color3;
	});
	registerKeyword("country_name", [this](const std::string& unused, std::istream& theStream) {
		name = CountryName(theStream).getName();
	});
	registerKeyword("currency_data", [this](const std::string& unused, std::istream& theStream) {
		CountryCurrencies currenciesFromBloc(theStream);
		currencies.manpower = currenciesFromBloc.getManpower();
		currencies.gold = currenciesFromBloc.getGold();
		currencies.stability = currenciesFromBloc.getStability();
		currencies.tyranny = currenciesFromBloc.getTyranny();
		currencies.war_exhaustion = currenciesFromBloc.getWarExhaustion();
		currencies.aggressive_expansion = currenciesFromBloc.getAggressiveExpansion();
		currencies.political_influence = currenciesFromBloc.getPoliticalInfluence();
		currencies.military_experience = currenciesFromBloc.getMilitaryExperience();
	});

	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);

}
