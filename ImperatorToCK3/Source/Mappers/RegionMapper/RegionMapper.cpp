#include "RegionMapper.h"
#include "../../Configuration/Configuration.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include "ParserHelpers.h"
#include <filesystem>
#include <fstream>

namespace fs = std::filesystem;

void mappers::RegionMapper::loadRegions(const Configuration& theConfiguration, CK3::LandedTitles& landedTitles)
{
	LOG(LogLevel::Info) << "-> Initializing Geography";
	
	auto regionFilename = theConfiguration.getCK3Path() + "/game/map_data/region.txt";
	auto islandRegionFilename = theConfiguration.getCK3Path() + "/game/map_data/island_region.txt";
	
	std::ifstream regionStream(fs::u8path(regionFilename));
	if (!regionStream.is_open())
		throw std::runtime_error("Could not open game/map_data/geographical_region.txt!");

	std::ifstream islandRegionStream(fs::u8path(islandRegionFilename));
	if (!islandRegionStream.is_open())
		throw std::runtime_error("Could not open game/map_data/island_region.txt!");
	
	loadRegions(landedTitles, regionStream, islandRegionStream);

	regionStream.close();
	islandRegionStream.close();
}

void mappers::RegionMapper::loadRegions(CK3::LandedTitles& landedTitles, std::istream& regionStream, std::istream& islandRegionStream)
{
	registerRegionKeys();
	parseStream(regionStream);
	clearRegisteredKeywords();

	registerRegionKeys();
	parseStream(islandRegionStream);
	clearRegisteredKeywords();
	
	for (const auto& [titleName, title] : landedTitles.getTitles())
	{
		if (titleName.starts_with("c_")) counties[titleName] = title;
		else if (titleName.starts_with("d_")) duchies[titleName] = title;
	}

	linkRegions();
}



void mappers::RegionMapper::registerRegionKeys()
{
	registerRegex("[\\w_]+", [this](const std::string& regionName, std::istream& theStream) {
		const auto newRegion = std::make_shared<Region>(theStream);
		regions[regionName] = newRegion;
	});
}


bool mappers::RegionMapper::provinceIsInRegion(const unsigned long long province, const std::string& regionName) const
{
	const auto& regionItr = regions.find(regionName);
	if (regionItr != regions.end() && regionItr->second)
		return regionItr->second->regionContainsProvince(province);

	// "Regions" are such a fluid term.
	const auto& duchyItr = duchies.find(regionName);
	if (duchyItr != duchies.end() && duchyItr->second)
		return duchyItr->second->duchyContainsProvince(province);

	// And sometimes they don't mean what people think they mean at all.
	const auto& countyItr = counties.find(regionName);
	if (countyItr != counties.end() && countyItr->second && countyItr->second->getCountyProvinces().contains(province))
		return true;

	return false;
}

std::optional<std::string> mappers::RegionMapper::getParentCountyName(const unsigned long long provinceID) const
{
	/*for (const auto& [duchyName, duchy] : duchies)
	{
		for (const auto& [vassalTitleName, vassalTitle] : duchy->getDeJureVassals())
		{
			if (vassalTitleName.starts_with("c_") && vassalTitle->getCountyProvinces().contains(provinceID))
				return vassalTitleName;
		}
	}*/
	for (const auto& [countyName, county] : counties)
	{
		if (county && county->getCountyProvinces().contains(provinceID))
			return countyName;
	}
	Log(LogLevel::Warning) << "Province ID " << provinceID << " has no parent county name!";
	return std::nullopt;
}

std::optional<std::string> mappers::RegionMapper::getParentDuchyName(const unsigned long long provinceID) const
{
	for (const auto& [duchyName, duchy]: duchies)
	{
		if (duchy && duchy->duchyContainsProvince(provinceID))
			return duchyName;
	}
	Log(LogLevel::Warning) << "Province ID " << provinceID << " has no parent duchy name!";
	return std::nullopt;
}

std::optional<std::string> mappers::RegionMapper::getParentRegionName(const unsigned long long provinceID) const
{
	for (const auto& [regionName, region]: regions)
	{	
		if (region && region->regionContainsProvince(provinceID))
		{
			return regionName;
		}
	}
	Log(LogLevel::Warning) << "Province ID " << provinceID << " has no parent region name!";
	return std::nullopt;
}


bool mappers::RegionMapper::regionNameIsValid(const std::string& regionName) const
{
	if (regions.contains(regionName))
		return true;

	// Who knows what the mapper needs. All kinds of stuff.
	if (duchies.contains(regionName))
		return true;
	if (counties.contains(regionName))
		return true;

	return false;
}

void mappers::RegionMapper::linkRegions()
{
	for (const auto& [regionName, region]: regions)
	{
		const auto& requiredDuchies = region->getDuchies();
		for (const auto& [requiredDuchyName, requiredDuchy]: requiredDuchies)
		{
			const auto& duchyItr = duchies.find(requiredDuchyName);
			if (duchyItr != duchies.end())
			{
				region->linkDuchy(duchyItr->second);
			}
			else
			{
				throw std::runtime_error("Region's " + regionName + " duchy " + requiredDuchyName + " does not exist!");
			}
		}
	}
}
/*
void mappers::RegionMapper::linkRegionsToRegions(const std::map<int, std::shared_ptr<Region>>& theRegions)
{
	for (const auto& region : regions)
	{
		const auto& rregions = region.second->getRegions();
		for (const auto& requiredRegion : rregions)
		{
			const auto& regionItr = regions.find(requiredRegion.first);
			if (regionItr != regions.end())
			{
				region.second->linkRegion(std::pair(regionItr->first, regionItr->second));
			}
			else
			{
				throw std::runtime_error("Region's " + region.first + " duchy " + requiredRegion.first + " does not exist!");
			}
		}
	}
}
*/

/*
void mappers::RegionMapper::linkProvinces(const std::map<int, std::shared_ptr<CK3::Province>>& theProvinces)
{
	for (const auto& county: counties)
	{
		const auto& rprovinces = county.second->getProvinces();
		for (const auto& requiredProvince: rprovinces)
		{
			const auto& provinceItr = theProvinces.find(requiredProvince.first);
			if (provinceItr != theProvinces.end())
			{
				county.second->linkProvince(std::pair(provinceItr->first, provinceItr->second));
			}
			else
			{
				throw std::runtime_error("County's " + county.first + " province " + std::to_string(requiredProvince.first) + " does not exist!");
			}
		}
	}
}
*/