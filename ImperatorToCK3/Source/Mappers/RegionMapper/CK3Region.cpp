#include "CK3Region.h"
#include "ParserHelpers.h"
#include "Log.h"

mappers::CK3Region::CK3Region(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::CK3Region::registerKeys()
{
	registerKeyword("regions", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::stringList names(theStream);
		for (const auto& name: names.getStrings())
			regions.emplace(name, nullptr);
	});
	registerKeyword("duchies", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::stringList names(theStream);
		for (const auto& name : names.getStrings())
		{
			duchies.emplace(name, nullptr);
		}
	});
	registerKeyword("counties", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::stringList names(theStream);
		for (const auto& name : names.getStrings())
			counties.emplace(name, nullptr);
	});
	registerKeyword("provinces", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::ullongList ids(theStream);
		for (const auto& id : ids.getULlongs())
			provinces.insert(id);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

bool mappers::CK3Region::regionContainsProvince(const unsigned long long province) const
{
	for (const auto& [regionName, region]: regions)
		if (region && region->regionContainsProvince(province))
			return true;

	for (const auto& [duchyName, duchy] : duchies)
	{
		if (duchy && duchy->duchyContainsProvince(province))
			return true;
	}
		
	for (const auto& [countyName, county] : counties)
		if (county && county->getCountyProvinces().contains(province))
			return true;
	
	if (provinces.contains(province))
		return true;
	
	return false;
}
