#include "ReligionMapping.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "Log.h"
#include "Mappers/RegionMapper/ImperatorRegionMapper.h"
#include "Mappers/RegionMapper/CK3RegionMapper.h"

mappers::ReligionMapping::ReligionMapping(std::istream& theStream)
{
	registerKeyword("ck3", [this](std::istream& theStream) {
		ck3Religion = commonItems::getString(theStream);
	});
	registerKeyword("imp", [this](std::istream& theStream) {
		impReligions.insert(commonItems::getString(theStream));
	});
	registerKeyword("ck3Region", [this](std::istream& theStream) {
		ck3Regions.insert(commonItems::getString(theStream));
	});
	registerKeyword("impRegion", [this](std::istream& theStream) {
		imperatorRegions.insert(commonItems::getString(theStream));
	});
	registerKeyword("ck3Province", [this](std::istream& theStream) {
		ck3Provinces.insert(commonItems::getULlong(theStream));
	});
	registerKeyword("impProvince", [this](std::istream& theStream) {
		imperatorProvinces.insert(commonItems::getULlong(theStream));
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}

std::optional<std::string> mappers::ReligionMapping::match(const std::string& impReligion,
                                                                   const unsigned long long ck3ProvinceID, const unsigned long long impProvinceID) const
{
	// We need at least a viable Imperator religion
	if (impReligion.empty())
		return std::nullopt;

	if (!impReligions.contains(impReligion))
		return std::nullopt;

	// simple religion-religion match
	if (ck3Provinces.empty() && imperatorProvinces.empty() && ck3Regions.empty() && imperatorRegions.empty())
		return ck3Religion;

	if (!ck3ProvinceID && !impProvinceID)
		return std::nullopt;

	// This is a CK3 provinces check
	if (ck3Provinces.contains(ck3ProvinceID))
		return ck3Religion;
	// This is a CK3 regions check, it checks if provided ck3Province is within the mapping's ck3Regions
	for (const auto& region : ck3Regions)
	{
		if (!ck3RegionMapper->regionNameIsValid(region))
		{
			Log(LogLevel::Warning) << "Checking for religion " << impReligion << " inside invalid CK3 region: " << region << "! Fix the mapping rules!";
			// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
			// for the converter to explode across the logs with invalid names. So, continue.
			continue;
		}
		if (ck3RegionMapper->provinceIsInRegion(ck3ProvinceID, region))
			return ck3Religion;
	}

	// This is an Imperator provinces check
	if (imperatorProvinces.contains(impProvinceID))
		return ck3Religion;
	// This is an Imperator regions check, it checks if provided impProvince is within the mapping's imperatorRegions
	for (const auto& region : imperatorRegions)
	{
		if (!imperatorRegionMapper->regionNameIsValid(region))
		{
			Log(LogLevel::Warning) << "Checking for religion " << impReligion << " inside invalid Imperator region: " << region << "! Fix the mapping rules!";
			// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
			// for the converter to explode across the logs with invalid names. So, continue.
			continue;
		}
		if (imperatorRegionMapper->provinceIsInRegion(impProvinceID, region))
			return ck3Religion;
	}
		
	return std::nullopt;
}
