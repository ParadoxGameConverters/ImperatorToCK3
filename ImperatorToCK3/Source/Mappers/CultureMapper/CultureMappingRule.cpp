#include "CultureMappingRule.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "Mappers/RegionMapper/ImperatorRegionMapper.h"
#include "Mappers/RegionMapper/CK3RegionMapper.h"

mappers::CultureMappingRule::CultureMappingRule(std::istream& theStream)
{
	registerKeyword("ck3", [this](std::istream& theStream) {
		destinationCulture = commonItems::getString(theStream);
	});
	registerKeyword("imp", [this](std::istream& theStream) {
		cultures.insert(commonItems::getString(theStream));
	});
	registerKeyword("religion", [this](std::istream& theStream) {
		religions.insert(commonItems::getString(theStream));
	});
	registerKeyword("owner", [this](std::istream& theStream) {
		owners.insert(commonItems::getString(theStream));
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

std::optional<std::string> mappers::CultureMappingRule::match(const std::string& impCulture,
	const std::string& CK3religion,
	const unsigned long long ck3ProvinceID,
	const unsigned long long impProvinceID,
	const std::string& CK3ownerTitle) const
{
	// We need at least a viable impCulture.
	if (impCulture.empty())
		return std::nullopt;

	if (!cultures.contains(impCulture))
		return std::nullopt;

	if (!owners.empty())
		if (CK3ownerTitle.empty() || !owners.contains(CK3ownerTitle))
			return std::nullopt;

	if (!religions.empty())
	{
		if (CK3religion.empty() || !religions.contains(CK3religion)) // (CK3 religion empty) or (CK3 religion not empty but not found in religions)
			return std::nullopt;
	}

	// simple culture-culture match
	if (ck3Provinces.empty() && imperatorProvinces.empty() && ck3Regions.empty() && imperatorRegions.empty())
		return destinationCulture;
	
	if (!ck3ProvinceID && !impProvinceID)
		return std::nullopt;

	// This is a CK3 provinces check
	if (ck3Provinces.contains(ck3ProvinceID))
		return destinationCulture;
	// This is a CK3 regions check, it checks if provided ck3Province is within the mapping's ck3Regions
	for (const auto& region : ck3Regions)
	{
		if (!ck3RegionMapper->regionNameIsValid(region))
		{
			Log(LogLevel::Warning) << "Checking for culture " << impCulture << " inside invalid CK3 region: " << region << "! Fix the mapping rules!";
			// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
			// for the converter to explode across the logs with invalid names. So, continue.
			continue;
		}
		if (ck3RegionMapper->provinceIsInRegion(ck3ProvinceID, region))
			return destinationCulture;
	}

	// This is an Imperator provinces check
	if (imperatorProvinces.contains(impProvinceID))
		return destinationCulture;
	// This is an Imperator regions check, it checks if provided impProvince is within the mapping's imperatorRegions
	for (const auto& region : imperatorRegions)
	{
		if (!imperatorRegionMapper->regionNameIsValid(region))
		{
			Log(LogLevel::Warning) << "Checking for religion " << impCulture << " inside invalid Imperator region: " << region << "! Fix the mapping rules!";
			// We could say this was a match, and thus pretend this region entry doesn't exist, but it's better
			// for the converter to explode across the logs with invalid names. So, continue.
			continue;
		}
		if (imperatorRegionMapper->provinceIsInRegion(impProvinceID, region))
			return destinationCulture;
	}

	return std::nullopt;
}

std::optional<std::string> mappers::CultureMappingRule::nonReligiousMatch(const std::string& impCulture,
	const std::string& CK3religion,
	const unsigned long long ck3ProvinceID,
	const unsigned long long impProvinceID,
	const std::string& CK3ownerTitle) const
{
	// This is a non religious match. We need a mapping without any religion, so if the
	// mapping rule has any religious qualifiers it needs to fail.
	if (!religions.empty())
		return std::nullopt;

	// Otherwise, as usual.
	return match(impCulture, CK3religion, ck3ProvinceID, impProvinceID, CK3ownerTitle);
}
