#include "Region.h"
#include "Duchy.h"
#include "County.h"
#include "ParserHelpers.h"

#include "Log.h" // TODO: remove

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
			regions.insert(std::pair(name, nullptr));
	});
	registerKeyword("duchies", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::stringList names(theStream);
		for (const auto& name : names.getStrings())
		{
			Log(LogLevel::Debug) << name;
			duchies.insert(std::pair(name, nullptr));
		}
	});
	/*registerKeyword("counties", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::stringList names(theStream);
		for (const auto& name : names.getStrings())
			counties.insert(std::pair(name, nullptr));
		});*/
	registerKeyword("provinces", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::intList names(theStream);
		for (const auto& name : names.getInts())
			provinces.insert(static_cast<unsigned int>(name));
		
		});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

bool mappers::Region::regionContainsProvince(int province) const
{
	for (const auto& region: regions)
		if (region.second != nullptr && region.second->regionContainsProvince(province))
			return true;

	for (const auto& duchy : duchies)
		if (duchy.second != nullptr && duchy.second->duchyContainsProvince(province))
			return true;

	/*for (const auto& county : counties)
		if (county.second->countyContainsProvince(province))
			return true;*/
	
	if (provinces.count(province))
		return true;
	
	return false;
}
