#include "CountryFactory.h"
#include "CountryCurrencies.h"
#include "CountryName.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"




std::string monarchyLawRegexStr = "succession_law|monarchy_military_reforms|monarchy_maritime_laws|monarchy_economic_law|monarchy_citizen_law"
"|monarchy_religious_laws|monarchy_legitimacy_laws|monarchy_contract_law|monarchy_divinity_statutes|jewish_monarchy_divinity_statutes|monarchy_subject_laws";
std::string republicLawRegexStr = "republic_military_recruitment_laws_rom|republic_election_reforms_rom|corruption_laws_rom|republican_mediterranean_laws_rom|republican_religious_laws_rom|republic_integration_laws_rom|republic_citizen_laws_rom|republican_land_reforms_rom"
"|republic_military_recruitment_laws|republic_election_reforms|corruption_laws|republican_mediterranean_laws|republican_religious_laws|republic_integration_laws|republic_citizen_laws|republican_land_reforms";
std::string tribalLawRegexStr = "tribal_religious_law|tribal_currency_laws|tribal_centralization_law|tribal_authority_laws|tribal_autonomy_laws|tribal_domestic_laws"
"|tribal_decentralized_laws|tribal_centralized_laws|tribal_super_decentralized_laws|tribal_super_centralized_laws";


std::set<std::string> monarchyGovernments{ "dictatorship", "despotic_monarchy", "aristocratic_monarchy", "stratocratic_monarchy", "theocratic_monarchy", "plutocratic_monarchy", "imperium", "imperial_cult" };
std::set<std::string> republicGovernments{ "aristocratic_republic", "theocratic_republic", "oligarchic_republic", "democratic_republic", "plutocratic_republic", "athenian_republic" };
std::set<std::string> tribalGovernments{ "tribal_chiefdom", "tribal_kingdom", "tribal_federation" };


Imperator::Country::Factory::Factory() {
	registerKeyword("tag", [this](std::istream& theStream){
		country->tag = commonItems::getString(theStream);
	});
	registerKeyword("country_name", [this](std::istream& theStream) {
		//country->countryName = std::move(*countryNameFactory.getCountryName(theStream));
		country->countryName = *countryNameFactory.getCountryName(theStream);
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
		else {
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
		auto governmentStr = commonItems::getString(theStream);
		country->government = governmentStr;
		// set government type
		if (monarchyGovernments.contains(governmentStr))
			country->governmentType = GovernmentType::monarchy;
		else if (republicGovernments.contains(governmentStr))
			country->governmentType = GovernmentType::republic;
		else if (tribalGovernments.contains(governmentStr))
			country->governmentType = GovernmentType::tribal;
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
	registerRegex(monarchyLawRegexStr, [this](const std::string& unused, std::istream& theStream) {
		country->monarchyLaws.emplace(commonItems::getString(theStream));
	});
	registerRegex(republicLawRegexStr, [this](const std::string& unused, std::istream& theStream) {
		country->republicLaws.emplace(commonItems::getString(theStream));
	});
	registerRegex(tribalLawRegexStr, [this](const std::string& unused, std::istream& theStream) {
		country->tribalLaws.emplace(commonItems::getString(theStream));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


std::unique_ptr<Imperator::Country> Imperator::Country::Factory::getCountry(std::istream& theStream, const unsigned long long countryID) {
	country = std::make_unique<Country>();
	country->ID = countryID;

	parseStream(theStream);

	return std::move(country);
}