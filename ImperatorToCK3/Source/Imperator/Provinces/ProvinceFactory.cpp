#include "ProvinceFactory.h"
#include "ProvinceName.h"
#include "CommonRegexes.h"
#include "Log.h"
#include "ParserHelpers.h"
#include <numeric>



Imperator::Province::Factory::Factory() {
	registerKeyword("province_name", [this](std::istream& theStream) {	
		province->name = ProvinceName{ theStream }.getName();	
	});	
	registerKeyword("culture", [this](std::istream& theStream) {	
		province->culture = commonItems::getString(theStream);	
	});	
	registerKeyword("religion", [this](std::istream& theStream) {	
		province->religion = commonItems::getString(theStream);	
	});	
	registerKeyword("owner", [this](std::istream& theStream) {	
		province->ownerCountry.first = commonItems::getULlong(theStream);	
	});	
	registerKeyword("controller", [this](std::istream& theStream) {	
		province->controller = commonItems::getULlong(theStream);	
	});
	registerKeyword("pop", [this](std::istream& theStream) {
		province->pops.emplace(commonItems::getULlong(theStream), nullptr);
	});
	registerKeyword("civilization_value", [this](std::istream& theStream) {
		const auto valStr = commonItems::getString(theStream);
		const auto result = std::stoul(valStr); // TODO: replace this with commonItems::stringToInteger<unsigned int> when it's brought back with GCC 11
		if (result > std::numeric_limits<unsigned>::max()) {
			throw std::out_of_range("stou out of range for " + valStr);
		}
		province->civilizationValue = result;
	});
	registerKeyword("province_rank", [this](std::istream& theStream) {
		const auto provinceRankStr = commonItems::getString(theStream);
		if (provinceRankStr == "settlement")
			province->provinceRank = ProvinceRank::settlement;
		else if (provinceRankStr == "city")
			province->provinceRank = ProvinceRank::city;
		else if (provinceRankStr == "city_metropolis")
			province->provinceRank = ProvinceRank::city_metropolis;
		else
			Log(LogLevel::Warning) << "Unknown province rank for province " << province->ID << ": " << provinceRankStr;
	});
	registerKeyword("fort", [this](std::istream& theStream) {
		province->fort = commonItems::getString(theStream) == "yes";
	});
	registerKeyword("holy_site", [this](std::istream& theStream) {
		province->holySite = commonItems::getULlong(theStream) != 4294967295; // 4294967295 is 2^32 − 1 and is the default value
	});
	registerKeyword("buildings", [this](std::istream& theStream) {
		const auto buildingsVector = commonItems::getInts(theStream);
		province->buildingsCount = std::accumulate(buildingsVector.begin(), buildingsVector.end(), 0);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


std::unique_ptr<Imperator::Province> Imperator::Province::Factory::getProvince(std::istream& theStream, const unsigned long long provID) {
	province = std::make_unique<Province>();
	province->ID = provID;

	parseStream(theStream);

	return std::move(province);
}