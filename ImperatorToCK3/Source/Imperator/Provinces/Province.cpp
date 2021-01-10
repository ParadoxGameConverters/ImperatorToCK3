#include "Province.h"
#include "Pop.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "ProvinceName.h"
#include <numeric>

Imperator::Province::Province(std::istream& theStream, const unsigned long long provID): provinceID(provID)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::Province::registerKeys()
{
	registerKeyword("province_name", [this](std::istream& theStream) {
		name = ProvinceName{ theStream }.getName();
	});
	registerKeyword("culture", [this](std::istream& theStream) {
		culture = commonItems::getString(theStream);
	});
	registerKeyword("religion", [this](std::istream& theStream) {
		religion = commonItems::getString(theStream);
	});
	registerKeyword("owner", [this](std::istream& theStream) {
		owner = commonItems::getULlong(theStream);
	});
	registerKeyword("controller", [this](std::istream& theStream) {
		controller = commonItems::getULlong(theStream);
	});
	registerKeyword("pop", [this](std::istream& theStream) {
		pops.emplace(commonItems::getULlong(theStream), nullptr);
	});
	registerKeyword("province_rank", [this](std::istream& theStream) {
		const auto provinceRankStr = commonItems::getString(theStream);
		if (provinceRankStr == "settlement")
			provinceRank = ProvinceRank::settlement;
		else if (provinceRankStr == "city")
			provinceRank = ProvinceRank::city;
		else if (provinceRankStr == "city_metropolis")
			provinceRank = ProvinceRank::city_metropolis;
		else
			Log(LogLevel::Warning) << "Unknown province rank for province " << provinceID << ": " << provinceRankStr;
	});
	registerKeyword("fort", [this](std::istream& theStream) {
		fort = commonItems::getString(theStream) == "yes";
	});
	registerKeyword("holy_site", [this](std::istream& theStream) {
		holySite = commonItems::getULlong(theStream) != 4294967295; // 4294967295 is 2^32 − 1 and is the default value
	});
	registerKeyword("buildings", [this](std::istream& theStream) {
		const auto buildingsVector = commonItems::getInts(theStream);
		buildingsCount = std::accumulate(buildingsVector.begin(), buildingsVector.end(), 0);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
