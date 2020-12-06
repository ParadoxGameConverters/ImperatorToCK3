#include "Province.h"
#include "Pop.h"
#include "Log.h"
#include "ParserHelpers.h"
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
	registerKeyword("province_name", [this](const std::string& unused, std::istream& theStream) {
		name = ProvinceName{ theStream }.getName();
	});
	registerKeyword("culture", [this](const std::string& unused, std::istream& theStream) {
		culture = commonItems::singleString{ theStream }.getString();
	});
	registerKeyword("religion", [this](const std::string& unused, std::istream& theStream) {
		religion = commonItems::singleString{ theStream }.getString();
	});
	registerKeyword("owner", [this](const std::string& unused, std::istream& theStream) {
		owner = commonItems::singleULlong{ theStream }.getULlong();
	});
	registerKeyword("controller", [this](const std::string& unused, std::istream& theStream) {
		controller = commonItems::singleULlong{ theStream }.getULlong();
	});
	registerKeyword("pop", [this](const std::string& unused, std::istream& theStream) {
		pops.emplace(commonItems::singleULlong{ theStream }.getULlong(), nullptr);
	});
	registerKeyword("province_rank", [this](const std::string& unused, std::istream& theStream) {
		const auto provinceRankStr = commonItems::singleString{ theStream }.getString();
		if (provinceRankStr == "settlement")
			provinceRank = ProvinceRank::settlement;
		else if (provinceRankStr == "city")
			provinceRank = ProvinceRank::city;
		else if (provinceRankStr == "city_metropolis")
			provinceRank = ProvinceRank::city_metropolis;
		else
			Log(LogLevel::Warning) << "Unknown province rank for province " << provinceID << ": " << provinceRankStr;
	});
	registerKeyword("buildings", [this](const std::string& unused, std::istream& theStream) {
		const auto buildingsVector = commonItems::intList{ theStream }.getInts();
		buildingsCount = std::accumulate(buildingsVector.begin(), buildingsVector.end(), 0);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
