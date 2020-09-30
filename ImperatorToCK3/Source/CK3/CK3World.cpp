#include "CK3World.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include <filesystem>
#include <fstream>
namespace fs = std::filesystem;
#include "../Imperator/Characters/Character.h"
#include "../Imperator/Countries/Country.h"
#include "../Imperator/Provinces/Province.h"
#include "../Configuration/Configuration.h"
#include <cmath>
#include "Province/CK3Provinces.h"
#include "Province/CK3ProvinceMappings.h"
#include "Titles/Title.h"

CK3::World::World(const ImperatorWorld::World& impWorld, const Configuration& theConfiguration, const mappers::VersionParser& versionParser): outputModName(impWorld.getSaveName())
{
	LOG(LogLevel::Info) << "*** Hello CK3, let's get painting. ***";
	// Scraping localizations from Imperator so we may know proper names for our countries.
	localizationMapper.scrapeLocalizations(theConfiguration, std::map<std::string, std::string>()); // passes an empty map as second arg because we don't actually load mods yet

	// Loading Imperator CoAs to use them for generated CK3 titles
	coaMapper = mappers::CoaMapper(theConfiguration);

	importImperatorCharacters(impWorld);

	// Loading vanilla CK3 landed titles
	landedTitles.loadTitles(theConfiguration.getCK3Path() + "/game/common/landed_titles/00_landed_titles.txt");
	// Load vanilla titles history
	titlesHistory = TitlesHistory(theConfiguration);
	
	importImperatorCountries(impWorld);
	
	// Now we can deal with provinces since we know to whom to assign them. We first import vanilla province data.
	// Some of it will be overwritten, but not all.
	importVanillaProvinces(theConfiguration.getCK3Path());

	// Next we import Imperator provinces and translate them ontop a significant part of all imported provinces.
	importImperatorProvinces(impWorld);

	linkCountiesToTitleHolders(impWorld);
}

void CK3::World::importImperatorCharacters(const ImperatorWorld::World& sourceWorld)
{
	LOG(LogLevel::Info) << "-> Importing Imperator Characters";

	for (const auto& character : sourceWorld.getCharacters())
	{
		importImperatorCharacter(character);
	}
	LOG(LogLevel::Info) << ">> " << characters.size() << " total characters recognized.";
}
void CK3::World::importImperatorCharacter(const std::pair<int, std::shared_ptr<ImperatorWorld::Character>>& character)
{
	// Create a new CK3 character
	auto newCharacter = std::make_shared<Character>();
	newCharacter->initializeFromImperator(character.second, religionMapper, cultureMapper, localizationMapper);
	character.second->registerCK3Character(newCharacter);
	characters.insert(std::pair(newCharacter->ID, newCharacter));
}

void CK3::World::importImperatorCountries(const ImperatorWorld::World& sourceWorld)
{
	LOG(LogLevel::Info) << "-> Importing Imperator Countries";

	// countries holds all tags imported from CK3. We'll now overwrite some and
	// add new ones from ck2 titles.
	for (const auto& title : sourceWorld.getCountries())
	{
		importImperatorCountry(title);
	}
	LOG(LogLevel::Info) << ">> " << titles.size() << " total countries recognized.";
}

void CK3::World::importImperatorCountry(const std::pair<int, std::shared_ptr<ImperatorWorld::Country>>& country)
{
	auto title = tagTitleMapper.getTitleForTag(country.second->getTag());
	if (!title)
		throw std::runtime_error("Country " + country.second->getTag() + " could not be mapped!");

	// Create a new title
	auto newTitle = std::make_shared<Title>();
	newTitle->initializeFromTag(*title, country.second, localizationMapper, landedTitles, provinceMapper, coaMapper);
	country.second->registerCK3Title(std::pair(*title, newTitle));
	titles.insert(std::pair(*title, newTitle));
}


void CK3::World::importVanillaProvinces(const std::string& ck3Path)
{
	LOG(LogLevel::Info) << "-> Importing Vanilla Provinces";
	// ---- Loading history/provinces
	auto fileNames = commonItems::GetAllFilesInFolderRecursive(ck3Path + "/game/history/provinces");
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
	fileNames = commonItems::GetAllFilesInFolderRecursive(ck3Path + "/game/history/province_mapping");
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
			Log(LogLevel::Warning) << "Could not determine source province for CK3 province " << province.first;
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
			Log(LogLevel::Warning) << "Source province " << imperatorProvinceID << " is not in the list of known provinces!";
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

void CK3::World::linkCountiesToTitleHolders(const ImperatorWorld::World& sourceWorld)
{
	for (const auto& [name, landedTitle] : landedTitles.getFoundTitles())
	{
		if (name.find("c_")==0) // title is a county
		{
			auto countyTitle = std::make_shared<Title>();
			countyTitle->titleName = name;
			auto capitalBarony = landedTitle.capitalBarony;
			if (capitalBarony)
			{
				auto owner = provinces.find(*capitalBarony)->second->getOwner();
				if (owner != 0)
				{
					std::optional<int> impMonarch;
					if (sourceWorld.getCountries().find(owner) != sourceWorld.getCountries().end()) impMonarch = sourceWorld.getCountries().find(owner)->second->getMonarch();
					if (impMonarch) countyTitle->holder = *impMonarch;
				}
				else // county is probably outside of Imperator map
				{
					auto vanillaHistory = titlesHistory.popTitleHistory(name);
					if (vanillaHistory) countyTitle->historyString = *vanillaHistory;
				}
			}
			titles.insert(std::pair(name, countyTitle));
		}
	}
}
