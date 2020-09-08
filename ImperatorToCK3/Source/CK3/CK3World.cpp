#include "CK3World.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include <filesystem>
#include <fstream>
namespace fs = std::filesystem;
#include "../Imperator/Characters/Character.h"
#include "../Imperator/Provinces/Province.h"
#include "../Configuration/Configuration.h"
#include <cmath>
#include "Province/CK3Provinces.h"
#include "Province/CK3ProvinceMappings.h"

CK3::World::World(const ImperatorWorld::World& impWorld, const Configuration& theConfiguration, const mappers::VersionParser& versionParser): outputModName(impWorld.getSaveName())
{
	// Now we can deal with provinces since we know to whom to assign them. We first import vanilla province data.
	// Some of it will be overwritten, but not all.
	importVanillaProvinces(theConfiguration.getCK3Path());

	// Next we import Imperator provinces and translate them ontop a significant part of all imported provinces.
	importImperatorProvinces(impWorld);
}


void CK3::World::importVanillaProvinces(const std::string& ck3Path)
{
	LOG(LogLevel::Info) << "-> Importing Vanilla Provinces";
	// ---- Loading history/provinces
	auto fileNames = Utils::GetAllFilesInFolder(ck3Path + "/game/history/provinces/");
	for (const auto& fileName : fileNames)
	{
		if (fileName.find(".txt") == std::string::npos)
			continue;
		try
		{
			auto newProvinces = Provinces(ck3Path + "/game/history/provinces/" + fileName);
			for (const auto& newProvince : newProvinces.getProvinces())
			{
				const auto id = newProvince.first;
				if (provinces.count(id))
				{
					Log(LogLevel::Warning) << "Vanilla province duplication - " << id << " already loaded! Overwriting.";
					provinces[id] = newProvince.second;
				}
				else
					provinces.insert(std::pair(id, newProvince.second));
			}
		}
		catch (std::exception& e)
		{
			Log(LogLevel::Warning) << "Invalid province filename: " << ck3Path << "/game/history/provinces/" << fileName << " : " << e.what();
		}
	}
	
	// now load the provinces that don't have unique entries in history/provinces
	// they instead use history/province_mapping
	fileNames = Utils::GetAllFilesInFolder(ck3Path + "/game/history/province_mapping/");
	for (const auto& fileName : fileNames)
	{
		if (fileName.find(".txt") == std::string::npos)
			continue;
		try
		{
			auto newProvinces = ProvinceMappings(ck3Path + "/game/history/province_mapping/" + fileName);
			for (const auto& newProvince : newProvinces.getMappings())
			{
				const auto id = newProvince.first;
				if (provinces.find(newProvince.second) == provinces.end())
				{
					Log(LogLevel::Warning) << "Base province " << newProvince.second << " not found for province " << id << ".";
					continue;
				}
				if (provinces.count(id))
				{
					Log(LogLevel::Info) << "Vanilla province duplication - " << id << " already loaded! Preferring unique entry over mapping.";
				}
				else
					provinces.insert(std::pair(id, provinces.find(newProvince.second)->second));
			}
		}
		catch (std::exception& e)
		{
			Log(LogLevel::Warning) << "Invalid province filename: " << ck3Path << "/game/history/province_mapping/" << fileName << " : " << e.what();
		}
	}
	LOG(LogLevel::Info) << ">> Loaded " << provinces.size() << " province definitions.";
}

void CK3::World::importImperatorProvinces(const ImperatorWorld::World& sourceWorld)
{
	LOG(LogLevel::Info) << "-> Importing Imperator Provinces";
	auto counter = 0;
	// Imperator provinces map to a subset of CK3 provinces. We'll only rewrite those we are responsible for.
	for (const auto& province : provinces)
	{
		const auto& impProvinces = provinceMapper.getImperatorProvinceNumbers(province.first);
		// Provinces we're not affecting will not be in this list.
		if (impProvinces.empty())
			continue;
		// Next, we find what province to use as its initializing source.
		const auto& sourceProvince = determineProvinceSource(impProvinces, sourceWorld);
		if (!sourceProvince)
		{
			continue; // MISMAP, or simply have mod provinces loaded we're not using.
		}
		else
		{
			province.second->initializeFromImperator(sourceProvince->second, cultureMapper, religionMapper);
		}
		// And finally, initialize it.
		counter++;
	}
	LOG(LogLevel::Info) << ">> " << sourceWorld.getProvinces().size() << " Imperator provinces imported into " << counter << " CK3 provinces.";
}

std::optional<std::pair<int, std::shared_ptr<ImperatorWorld::Province>>> CK3::World::determineProvinceSource(const std::vector<int>& impProvinceNumbers,
	const ImperatorWorld::World& sourceWorld) const
{
	// determine ownership by province development.
	std::map<int, std::vector<std::shared_ptr<ImperatorWorld::Province>>> theClaims; // owner, offered province sources
	std::map<int, int> theShares;														// owner, development
	int winner = -1;
	auto maxDev = -1;

	for (auto imperatorProvinceID : impProvinceNumbers)
	{
		const auto& impProvince = sourceWorld.getProvinces().find(imperatorProvinceID);
		if (impProvince == sourceWorld.getProvinces().end())
		{
			continue; // Broken mapping, or loaded a mod changing provinces without using it.
		}
		auto owner = impProvince->second->getOwner();
		theClaims[owner].push_back(impProvince->second);
		theShares[owner] = lround(impProvince->second->getBuildingsCount() + impProvince->second->getPopCount());
	}
	// Let's see who the lucky winner is.
	for (const auto& share : theShares)
	{
		if (share.second > maxDev)
		{
			winner = share.first;
			maxDev = share.second;
		}
	}
	if (winner == -1)
	{
		return std::nullopt;
	}

	// Now that we have a winning owner, let's find its largest province to use as a source.
	maxDev = -1; // We can have winning provinces with weight = 0;

	std::pair<int, std::shared_ptr<ImperatorWorld::Province>> toReturn;
	for (const auto& province : theClaims[winner])
	{
		const auto provinceWeight = province->getBuildingsCount() + province->getPopCount();

		if (static_cast<int>(provinceWeight) > maxDev)
		{
			toReturn.first = province->getID();
			toReturn.second = province;
			maxDev = provinceWeight;
		}
	}
	if (!toReturn.first || !toReturn.second)
	{
		return std::nullopt;
	}
	return toReturn;
}