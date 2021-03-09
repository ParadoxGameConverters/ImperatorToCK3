#include "CK3RegionMapper.h"
#include "CK3/Titles/Title.h"
#include "CK3/Titles/LandedTitles.h"
#include "CK3Region.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include <filesystem>
#include <fstream>



namespace fs = std::filesystem;



mappers::CK3RegionMapper::CK3RegionMapper(const std::string& ck3Path, CK3::LandedTitles& landedTitles) {
	LOG(LogLevel::Info) << "-> Initializing Geography";
	
	auto regionFilename = ck3Path + "/game/map_data/geographical_region.txt";
	auto islandRegionFilename = ck3Path + "/game/map_data/island_region.txt";
	
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


void mappers::CK3RegionMapper::loadRegions(CK3::LandedTitles& landedTitles, std::istream& regionStream, std::istream& islandRegionStream) {
	registerRegionKeys();
	parseStream(regionStream);
	parseStream(islandRegionStream);
	clearRegisteredKeywords();
	
	for (const auto& [titleName, title] : landedTitles.getTitles()) {
		if (titleName.starts_with("c_"))
			counties[titleName] = title;
		else if (titleName.starts_with("d_"))
			duchies[titleName] = title;
	}

	linkRegions();
}



void mappers::CK3RegionMapper::registerRegionKeys() {
	registerRegex(R"([\w_&]+)", [this](const std::string& regionName, std::istream& theStream) {
		const auto newRegion = std::make_shared<CK3Region>(theStream);
		regions[regionName] = newRegion;
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


bool mappers::CK3RegionMapper::provinceIsInRegion(const unsigned long long province, const std::string& regionName) const {
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


std::optional<std::string> mappers::CK3RegionMapper::getParentCountyName(const unsigned long long provinceID) const {
	for (const auto& [countyName, county] : counties) {
		if (county && county->getCountyProvinces().contains(provinceID))
			return countyName;
	}
	Log(LogLevel::Warning) << "Province ID " << provinceID << " has no parent county name!";
	return std::nullopt;
}


std::optional<std::string> mappers::CK3RegionMapper::getParentDuchyName(const unsigned long long provinceID) const {
	for (const auto& [duchyName, duchy]: duchies) {
		if (duchy && duchy->duchyContainsProvince(provinceID))
			return duchyName;
	}
	Log(LogLevel::Warning) << "Province ID " << provinceID << " has no parent duchy name!";
	return std::nullopt;
}


std::optional<std::string> mappers::CK3RegionMapper::getParentRegionName(const unsigned long long provinceID) const {
	for (const auto& [regionName, region]: regions) {
		if (region && region->regionContainsProvince(provinceID)) {
			return regionName;
		}
	}
	Log(LogLevel::Warning) << "Province ID " << provinceID << " has no parent region name!";
	return std::nullopt;
}


bool mappers::CK3RegionMapper::regionNameIsValid(const std::string& regionName) const
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


void mappers::CK3RegionMapper::linkRegions() {
	for (const auto& [regionName, region]: regions) {
		// regions
		for (const auto& [requiredRegionName, requiredRegion] : region->getRegions()) {
			const auto& regionItr = regions.find(requiredRegionName);
			if (regionItr != regions.end()) {
				region->linkRegion(regionItr->first, regionItr->second);
			}
			else {
				throw std::runtime_error("Region's " + regionName + " region " + requiredRegionName + " does not exist!");
			}
		}

		// duchies
		for (const auto& [requiredDuchyName, requiredDuchy]: region->getDuchies()) {
			const auto& duchyItr = duchies.find(requiredDuchyName);
			if (duchyItr != duchies.end()) {
				region->linkDuchy(duchyItr->second);
			}
			else {
				throw std::runtime_error("Region's " + regionName + " duchy " + requiredDuchyName + " does not exist!");
			}
		}

		// counties
		for (const auto& [requiredCountyName, requiredCounty] : region->getCounties()) {
			const auto& countyItr = counties.find(requiredCountyName);
			if (countyItr != counties.end()) {
				region->linkCounty(countyItr->second);
			}
			else {
				throw std::runtime_error("Region's " + regionName + " county " + requiredCountyName + " does not exist!");
			}
		}
	}
}