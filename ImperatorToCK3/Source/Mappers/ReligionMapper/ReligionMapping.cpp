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

	// This is a straight province check, not regions.
	if (!ck3Provinces.empty() && ck3Regions.empty())
		if (!ck3Provinces.contains(ck3ProvinceID))
			return std::nullopt;
	if (!imperatorProvinces.empty() && imperatorRegions.empty())
		if (!imperatorProvinces.contains(impProvinceID))
			return std::nullopt;

	// Asking for a regions check without an incoming province is pointless.
	if (!ck3Regions.empty() && !ck3ProvinceID)
		return std::nullopt;
	if (!imperatorRegions.empty() && !impProvinceID)
		return std::nullopt;

	// This is a CK3 regions check, that checks if a provided province is within that CK3 region.
	if (!ck3Regions.empty() || !imperatorRegions.empty())
	{
		if (!ck3RegionMapper)
			throw std::runtime_error("Religion Mapper: CK3 Region Mapper is unloaded!");
		if (!imperatorRegionMapper)
			throw std::runtime_error("Religion Mapper: Imperator Region Mapper is unloaded!");
		auto regionMatch = false;
		for (const auto& region : ck3Regions)
		{
			if (!ck3RegionMapper->regionNameIsValid(region))
			{
				Log(LogLevel::Warning) << "Checking for religion " << impReligion << " inside invalid region: " << region << "! Fix the mapping rules!";
				// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
				// for the converter to explode across the logs with invalid names. So, continue.
				continue;
			}
			if (ck3RegionMapper->provinceIsInRegion(ck3ProvinceID, region))
				regionMatch = true;
		}
		for (const auto& region : imperatorRegions)
		{
			if (!imperatorRegionMapper->regionNameIsValid(region))
			{
				Log(LogLevel::Warning) << "Checking for religion " << impReligion << " inside invalid region: " << region << "! Fix the mapping rules!";
				// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
				// for the converter to explode across the logs with invalid names. So, continue.
				continue;
			}
			if (imperatorRegionMapper->provinceIsInRegion(impProvinceID, region))
				regionMatch = true;
		}
		// This is an override if we have a province outside the regions specified.
		if (ck3Provinces.contains(ck3ProvinceID))
			regionMatch = true;
		if (imperatorProvinces.contains(impProvinceID))
			regionMatch = true;
		if (!regionMatch)
			return std::nullopt;
	}
	return ck3Religion;
}
