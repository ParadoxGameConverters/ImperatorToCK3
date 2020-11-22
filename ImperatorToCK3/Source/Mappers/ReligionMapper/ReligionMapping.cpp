#include "ReligionMapping.h"
#include "ParserHelpers.h"
#include "Log.h"
#include "Mappers/RegionMapper/ImperatorRegionMapper.h"
#include "Mappers/RegionMapper/CK3RegionMapper.h"

mappers::ReligionMapping::ReligionMapping(std::istream& theStream)
{
	registerKeyword("ck3", [this](const std::string& unused, std::istream& theStream) {
		ck3Religion = commonItems::singleString{ theStream }.getString();
	});
	registerKeyword("imp", [this](const std::string& unused, std::istream& theStream) {
		impReligions.insert(commonItems::singleString{ theStream }.getString());
	});
	registerKeyword("ck3Region", [this](const std::string& unused, std::istream& theStream) {
		ck3Regions.insert(commonItems::singleString{ theStream }.getString());
	});
	registerKeyword("impRegion", [this](const std::string& unused, std::istream& theStream) {
		imperatorRegions.insert(commonItems::singleString{ theStream }.getString());
	});
	registerKeyword("ck3Province", [this](const std::string& unused, std::istream& theStream) {
		ck3Provinces.insert(commonItems::singleULlong{ theStream }.getULlong());
	});
	registerKeyword("impProvince", [this](const std::string& unused, std::istream& theStream) {
		imperatorProvinces.insert(commonItems::singleULlong{ theStream }.getULlong());
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}

std::optional<std::string> mappers::ReligionMapping::religionMatch(const std::string& impReligion,
                                                                   const unsigned long long ck3ProvinceID, const unsigned long long impProvinceID) const
{
	// We need at least a viable Imperator religion
	if (impReligion.empty())
		return std::nullopt;

	if (!impReligions.contains(impReligion))
		return std::nullopt;

	if (!ck3Provinces.empty() || !imperatorProvinces.empty() || !ck3Regions.empty() || !imperatorRegions.empty())
	{
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
	
	// simple religion-religion match
	return ck3Religion;
}
