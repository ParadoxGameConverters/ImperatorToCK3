#include "Region.h"
#include "ParserHelpers.h"
#include "Log.h"

mappers::Region::Region(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::Region::registerKeys()
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
			Log(LogLevel::Debug) << name;
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

bool mappers::Region::regionContainsProvince(const unsigned long long province) const
{
	for (const auto& [regionName, region]: regions)
		if (region && region->regionContainsProvince(province))
			return true;

	Log(LogLevel::Debug) << duchies.size();
	for (const auto& [duchyName, duchy] : duchies)
	{		
		if (duchy) Log(LogLevel::Debug) << "regionContainsProvince found valid duchy " << duchyName;

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
