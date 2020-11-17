#include "ImperatorRegionMapper.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include <filesystem>
#include <fstream>
#include "ParserHelpers.h"

namespace fs = std::filesystem;

mappers::ImperatorRegionMapper::ImperatorRegionMapper(const std::string& imperatorPath)
{
	LOG(LogLevel::Info) << "-> Initializing Imperator Geography";
	
	auto areaFileName = imperatorPath + "/game/map_data/areas.txt";
	auto regionFileName = imperatorPath + "/game/map_data/regions.txt";
	
	std::ifstream areaStream(fs::u8path(areaFileName));
	if (!areaStream.is_open())
		throw std::runtime_error("Could not open game/map_data/areas.txt!");

	std::ifstream regionStream(fs::u8path(regionFileName));
	if (!regionStream.is_open())
		throw std::runtime_error("Could not open game/map_data/regions.txt!");
	
	loadRegions(areaStream, regionStream);

	areaStream.close();
	regionStream.close();
}

void mappers::ImperatorRegionMapper::loadRegions(std::istream& areaStream, std::istream& regionStream)
{
	registerAreaKeys();
	parseStream(areaStream);
	clearRegisteredKeywords();
	
	registerRegionKeys();
	parseStream(regionStream);
	clearRegisteredKeywords();

	linkRegions();
}



void mappers::ImperatorRegionMapper::registerRegionKeys()
{
	registerRegex(R"([\w_&]+)", [this](const std::string& regionName, std::istream& theStream) {
		const auto newRegion = std::make_shared<ImperatorRegion>(theStream);
		regions[regionName] = newRegion;
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
void mappers::ImperatorRegionMapper::registerAreaKeys()
{
	registerRegex(R"([\w_&]+)", [this](const std::string& areaName, std::istream& theStream) {
		const auto newArea = std::make_shared<ImperatorArea>(theStream);
		areas[areaName] = newArea;
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


bool mappers::ImperatorRegionMapper::provinceIsInRegion(const unsigned long long province, const std::string& regionName) const
{
	const auto& regionItr = regions.find(regionName);
	if (regionItr != regions.end() && regionItr->second)
		return regionItr->second->regionContainsProvince(province);

	// "Regions" are such a fluid term.
	const auto& areaItr = areas.find(regionName);
	if (areaItr != areas.end() && areaItr->second)
		return areaItr->second->areaContainsProvince(province);

	return false;
}

std::optional<std::string> mappers::ImperatorRegionMapper::getParentRegionName(const unsigned long long provinceID) const
{
	for (const auto& [regionName, region] : regions)
	{
		if (region && region->regionContainsProvince(provinceID))
		{
			return regionName;
		}
	}
	Log(LogLevel::Warning) << "Province ID " << provinceID << " has no parent region name!";
	return std::nullopt;
}


std::optional<std::string> mappers::ImperatorRegionMapper::getParentAreaName(const unsigned long long provinceID) const
{
	for (const auto& [areaName, area]: areas)
	{
		if (area && area->areaContainsProvince(provinceID))
			return areaName;
	}
	Log(LogLevel::Warning) << "Province ID " << provinceID << " has no parent area name!";
	return std::nullopt;
}



bool mappers::ImperatorRegionMapper::regionNameIsValid(const std::string& regionName) const
{
	if (regions.contains(regionName))
		return true;

	// Who knows what the mapper needs. All kinds of stuff.
	if (areas.contains(regionName))
		return true;

	return false;
}

void mappers::ImperatorRegionMapper::linkRegions()
{
	for (const auto& [regionName, region]: regions)
	{
		for (const auto& [requiredAreaName, requiredArea]: region->getAreas())
		{
			const auto& areaItr = areas.find(requiredAreaName);
			if (areaItr != areas.end())
			{
				region->linkArea(requiredAreaName, areaItr->second);
			}
			else
			{
				throw std::runtime_error("Region's " + regionName + " area " + requiredAreaName + " does not exist!");
			}
		}
	}
}