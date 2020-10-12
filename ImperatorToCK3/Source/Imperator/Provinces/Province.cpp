#include "Province.h"
#include "Pop.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "ProvinceName.h"
#include <numeric>

Imperator::Province::Province(std::istream& theStream, const int provID): provinceID(provID)
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
		const commonItems::singleInt ownerInt(theStream);
		owner = ownerInt.getInt();
	});
	registerKeyword("controller", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt controllerInt(theStream);
		controller = controllerInt.getInt();
	});
	registerRegex("pop", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt popInt(theStream);
		pops.insert(std::pair(popInt.getInt(), nullptr));
	});
	registerRegex("buildings", [this](const std::string& unused, std::istream& theStream) {
		const auto buildingsVector = commonItems::intList(theStream).getInts();
		buildingsCount = std::accumulate(buildingsVector.begin(), buildingsVector.end(), 0);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
