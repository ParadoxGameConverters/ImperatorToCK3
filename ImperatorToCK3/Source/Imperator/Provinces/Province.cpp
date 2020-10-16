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
		name = ProvinceName(theStream).getName();
	});
	registerKeyword("culture", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString cultureStr(theStream);
		culture = cultureStr.getString();
	});
	registerKeyword("religion", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString religionStr(theStream);
		religion = religionStr.getString();
	});
	registerKeyword("owner", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleULlong ownerULlong(theStream);
		owner = ownerULlong.getULlong();
	});
	registerKeyword("controller", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleULlong controllerULlong(theStream);
		controller = controllerULlong.getULlong();
	});
	registerRegex("pop", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleULlong popLongLong(theStream);
		pops.insert(std::pair(popLongLong.getULlong(), nullptr));
	});
	registerRegex("buildings", [this](const std::string& unused, std::istream& theStream) {
		const auto buildingsVector = commonItems::intList(theStream).getInts();
		buildingsCount = std::accumulate(buildingsVector.begin(), buildingsVector.end(), 0);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
