#include "CK3Region.h"
#include "CK3/Titles/Title.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "Log.h"
#include <ranges>



mappers::CK3Region::CK3Region(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void mappers::CK3Region::registerKeys() {
	registerKeyword("regions", [this](std::istream& theStream) {
		for (const auto& name : commonItems::getStrings(theStream))
			regions.emplace(name, nullptr);
	});
	registerKeyword("duchies", [this](std::istream& theStream) {
		for (const auto& name : commonItems::getStrings(theStream))
			duchies.emplace(name, nullptr);
	});
	registerKeyword("counties", [this](std::istream& theStream) {
		for (const auto& name : commonItems::getStrings(theStream))
			counties.emplace(name, nullptr);
	});
	registerKeyword("provinces", [this](std::istream& theStream) {
		for (const auto& id : commonItems::getULlongs(theStream))
			provinces.insert(id);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


bool mappers::CK3Region::regionContainsProvince(const unsigned long long province) const {
	for (const auto& region : regions | std::views::values)
		if (region && region->regionContainsProvince(province))
			return true;

	for (const auto& duchy : duchies | std::views::values)
		if (duchy && duchy->duchyContainsProvince(province))
			return true;
		
	for (const auto& county : counties | std::views::values)
		if (county && county->getCountyProvinces().contains(province))
			return true;
	
	if (provinces.contains(province))
		return true;
	
	return false;
}


void mappers::CK3Region::linkRegion(const std::string& regionName, const std::shared_ptr<CK3Region>& region) {
	regions[regionName] = region;
}


void mappers::CK3Region::linkDuchy(const std::shared_ptr<CK3::Title>& theDuchy) {
	duchies[theDuchy->getName()] = theDuchy;
}


void mappers::CK3Region::linkCounty(const std::shared_ptr<CK3::Title>& theCounty) {
	counties[theCounty->getName()] = theCounty;
}
