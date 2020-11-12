#include "RegionMapper.h"
#include "../../Configuration/Configuration.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include "ParserHelpers.h"
#include <filesystem>
namespace fs = std::filesystem;
#include <fstream>

void mappers::RegionMapper::loadRegions(const Configuration& theConfiguration)
{
	LOG(LogLevel::Info) << "-> Initializing Geography";
	
	auto landedTitlesFilename = theConfiguration.getCK3Path() + "/game/common/landed_titles/00_landed_titles.txt";
	auto regionFilename = theConfiguration.getCK3Path() + "/game/map_data/region.txt";
	auto islandRegionFilename = theConfiguration.getCK3Path() + "/game/map_data/island_region.txt";

	std::ifstream landedTitlesStream(fs::u8path(landedTitlesFilename));
	if (!landedTitlesStream.is_open())
		throw std::runtime_error("Could not open game/common/landed_titles/00_landed_titles.txt!");
	loadLandedTitles(landedTitlesStream);
	landedTitlesStream.close();
	
	std::ifstream regionStream(fs::u8path(regionFilename));
	if (!regionStream.is_open())
		throw std::runtime_error("Could not open game/map_data/geographical_region.txt!");
	registerRegionKeys();
	parseStream(regionStream);
	clearRegisteredKeywords();
	regionStream.close();

	std::ifstream islandRegionStream(fs::u8path(islandRegionFilename));
	if (!islandRegionStream.is_open())
		throw std::runtime_error("Could not open game/map_data/island_region.txt!");
	registerRegionKeys();
	parseStream(islandRegionStream);
	clearRegisteredKeywords();
	islandRegionStream.close();

	linkRegions();
}

void mappers::RegionMapper::loadRegions(std::istream& landedTitlesStream, std::istream& regionStream, std::istream& islandRegionStream)
{
	loadLandedTitles(landedTitlesStream);

	registerRegionKeys();
	parseStream(regionStream);
	clearRegisteredKeywords();

	registerRegionKeys();
	parseStream(islandRegionStream);
	clearRegisteredKeywords();

	linkRegions();
}


void mappers::RegionMapper::registerLandedTitlesKeys() // TODO: start from here
{
	
	registerRegex("(e|k)_[A-Za-z0-9_-]+", [this](const std::string& titleName, std::istream& theStream) {
		Log(LogLevel::Debug) << titleName;
		loadLandedTitles(theStream); // recursive
	});
	registerRegex("d_[A-Za-z0-9_-]+", [this](const std::string& duchyName, std::istream& theStream) {
		auto newDuchy = std::make_shared<Duchy>(theStream);
		duchies.insert(std::pair(duchyName, newDuchy));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

void mappers::RegionMapper::loadLandedTitles(std::istream& theStream)
{
	registerLandedTitlesKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::RegionMapper::registerRegionKeys()
{
	registerRegex("[\\w_]+", [this](const std::string& regionName, std::istream& theStream) {
		auto newRegion = std::make_shared<Region>(theStream);
		regions.insert(std::pair(regionName, newRegion));
	});
}


bool mappers::RegionMapper::provinceIsInRegion(int province, const std::string& regionName) const
{
	const auto& regionItr = regions.find(regionName);
	if (regionItr != regions.end() && regionItr->second != nullptr)
		return regionItr->second->regionContainsProvince(province);

	// "Regions" are such a fluid term.
	const auto& duchyItr = duchies.find(regionName);
	if (duchyItr != duchies.end() && duchyItr->second != nullptr)
		return duchyItr->second->duchyContainsProvince(province);

	// And sometimes they don't mean what people think they mean at all.
	const auto& countyItr = counties.find(regionName);
	if (countyItr != counties.end() && countyItr->second != nullptr)
		return countyItr->second->countyContainsProvince(province);

	return false;
}

std::optional<std::string> mappers::RegionMapper::getParentCountyName(const int provinceID) const
{
	for (const auto& county : counties)
	{
		if (county.second != nullptr && county.second->countyContainsProvince(provinceID))
			return county.first;
	}
	Log(LogLevel::Warning) << "Province ID " << provinceID << " has no parent county name!";
	return std::nullopt;
}

std::optional<std::string> mappers::RegionMapper::getParentDuchyName(const int provinceID) const
{
	for (const auto& duchy: duchies)
	{
		if (duchy.second != nullptr && duchy.second->duchyContainsProvince(provinceID))
			return duchy.first;
	}
	Log(LogLevel::Warning) << "Province ID " << provinceID << " has no parent duchy name!";
	return std::nullopt;
}
/*
std::optional<std::string> mappers::RegionMapper::getParentRegionName(const int provinceID) const
{
	void iterateOverRegions (const std::pair<const std::string, std::shared_ptr<Region>>& region)
	{
		
	}
	for (const auto& region: regions)
	{
		if (region.second != nullptr && region.second->regionContainsProvince(provinceID))
		{
			if (region.second->getRegions().empty()) return region.first;
			return 
		}
	}
	Log(LogLevel::Warning) << "Province ID " << provinceID << " has no parent region name!";
	return std::nullopt;
}
*/

bool mappers::RegionMapper::regionNameIsValid(const std::string& regionName) const
{
	const auto& regionItr = regions.find(regionName);
	if (regionItr != regions.end())
		return true;

	// Who knows what the mapper needs. All kinds of stuff.
	const auto& duchyItr = duchies.find(regionName);
	if (duchyItr != duchies.end())
		return true;
	const auto& countyItr = counties.find(regionName);
	if (countyItr != counties.end())
		return true;

	return false;
}

void mappers::RegionMapper::linkRegions()
{
	for (const auto& region: regions)
	{
		const auto& requiredDuchies = region.second->getDuchies();
		for (const auto& requiredDuchy: requiredDuchies)
		{
			const auto& duchyItr = duchies.find(requiredDuchy.first);
			if (duchyItr != duchies.end())
			{
				region.second->linkDuchy(std::pair(duchyItr->first, duchyItr->second));
			}
			else
			{
				throw std::runtime_error("Region's " + region.first + " duchy " + requiredDuchy.first + " does not exist!");
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