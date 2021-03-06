#include "CK3World.h"
#include "Imperator/ImperatorWorld.h"
#include "Imperator/Characters/Character.h"
#include "Imperator/Countries/Country.h"
#include "Imperator/Provinces/Province.h"
#include "Configuration/Configuration.h"
#include "Province/CK3Provinces.h"
#include "Province/CK3ProvinceMappings.h"
#include "Titles/Title.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include <filesystem>
#include <fstream>
#include <cmath>



namespace fs = std::filesystem;



CK3::World::World(const Imperator::World& impWorld, const Configuration& theConfiguration, const mappers::VersionParser& versionParser) {
	LOG(LogLevel::Info) << "*** Hello CK3, let's get painting. ***";
	// Scraping localizations from Imperator so we may know proper names for our countries.
	localizationMapper.scrapeLocalizations(theConfiguration, std::map<std::string, std::string>()); // passes an empty map as second arg because we don't actually load mods yet

	// Loading Imperator CoAs to use them for generated CK3 titles
	coaMapper = mappers::CoaMapper(theConfiguration);

	// Loading vanilla CK3 landed titles
	landedTitles.loadTitles(theConfiguration.getCK3Path() + "/game/common/landed_titles/00_landed_titles.txt");
	// Loading regions
	ck3RegionMapper = std::make_shared<mappers::CK3RegionMapper>(theConfiguration.getCK3Path(), landedTitles);
	imperatorRegionMapper = std::make_shared<mappers::ImperatorRegionMapper>(theConfiguration.getImperatorPath());
	// Use the region mappers in other mappers
	religionMapper.loadRegionMappers(imperatorRegionMapper, ck3RegionMapper);
	cultureMapper.loadRegionMappers(imperatorRegionMapper, ck3RegionMapper);
	
	// Load vanilla titles history
	titlesHistory = TitlesHistory(theConfiguration);

	importImperatorCountries(impWorld);

	// Now we can deal with provinces since we know to whom to assign them. We first import vanilla province data.
	// Some of it will be overwritten, but not all.
	importVanillaProvinces(theConfiguration.getCK3Path());

	// Next we import Imperator provinces and translate them ontop a significant part of all imported provinces.
	importImperatorProvinces(impWorld);


	importImperatorCharacters(impWorld, theConfiguration.getConvertBirthAndDeathDates(), impWorld.getEndDate());
	linkSpouses();
	linkMothersAndFathers();

	importImperatorFamilies(impWorld);


	addHoldersAndHistoryToTitles(impWorld);
	removeInvalidLandlessTitles();
}


void CK3::World::importImperatorCharacters(const Imperator::World& impWorld, const bool ConvertBirthAndDeathDates = true, const date endDate = date(867, 1, 1)) {
	LOG(LogLevel::Info) << "-> Importing Imperator Characters";

	for (const auto& character : impWorld.getCharacters()) {
		importImperatorCharacter(character, ConvertBirthAndDeathDates, endDate);
	}
	LOG(LogLevel::Info) << ">> " << characters.size() << " total characters recognized.";
}


void CK3::World::importImperatorCharacter(const std::pair<unsigned long long, std::shared_ptr<Imperator::Character>>& character, const bool ConvertBirthAndDeathDates = true, const date endDate = date(867, 1, 1)) {
	// Create a new CK3 character
	auto newCharacter = std::make_shared<Character>();
	newCharacter->initializeFromImperator(character.second, religionMapper, cultureMapper, traitMapper, nicknameMapper, localizationMapper, provinceMapper, ConvertBirthAndDeathDates, endDate);
	character.second->registerCK3Character(newCharacter);
	characters.emplace(newCharacter->ID, newCharacter);
}


void CK3::World::importImperatorCountries(const Imperator::World& impWorld) {
	LOG(LogLevel::Info) << "-> Importing Imperator Countries";

	// landedTitles holds all titles imported from CK3. We'll now overwrite some and
	// add new ones from Imperator tags.
	for (const auto& title : impWorld.getCountries()) {
		importImperatorCountry(title);
	}
	LOG(LogLevel::Info) << ">> " << getTitles().size() << " total countries recognized.";
}


void CK3::World::importImperatorCountry(const std::pair<unsigned long long, std::shared_ptr<Imperator::Country>>& country) {
	// Create a new title
	auto newTitle = std::make_shared<Title>();
	newTitle->initializeFromTag(country.second, localizationMapper, landedTitles, provinceMapper, coaMapper, tagTitleMapper, governmentMapper);
	
	const auto& name = newTitle->getName();
	if (auto titleItr = getTitles().find(name); titleItr!=getTitles().end()) {
		const auto& vanillaTitle = titleItr->second;
		vanillaTitle->updateFromTitle(newTitle);
		country.second->setCK3Title(vanillaTitle);
	}
	else {
		landedTitles.insertTitle(newTitle);
		country.second->setCK3Title(newTitle);
	}
}


void CK3::World::importVanillaProvinces(const std::string& ck3Path) {
	LOG(LogLevel::Info) << "-> Importing Vanilla Provinces";
	// ---- Loading history/provinces
	auto fileNames = commonItems::GetAllFilesInFolderRecursive(ck3Path + "/game/history/provinces");
	for (const auto& fileName : fileNames) {
		if (!fileName.ends_with(".txt"))
			continue;
		try {
			auto newProvinces = Provinces(ck3Path + "/game/history/provinces/" + fileName);
			for (const auto& [newProvinceID, newProvince] : newProvinces.getProvinces()) {
				const auto id = newProvinceID;
				if (auto provinceItr = provinces.find(id); provinceItr != provinces.end()) {
					Log(LogLevel::Warning) << "Vanilla province duplication - " << id << " already loaded! Overwriting.";
					provinceItr->second = newProvince;
				}
				else
					provinces.emplace(id, newProvince);
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
	for (const auto& fileName : fileNames) {
		if (!fileName.ends_with(".txt"))
			continue;
		try {
			auto newProvinces = ProvinceMappings(ck3Path + "/game/history/province_mapping/" + fileName);
			for (const auto& [newProvinceID, baseProvinceID] : newProvinces.getMappings()) {
				if (!provinces.contains(baseProvinceID)) {
					Log(LogLevel::Warning) << "Base province " << baseProvinceID << " not found for province " << newProvinceID << ".";
					continue;
				}
				if (provinces.contains(newProvinceID)) {
					Log(LogLevel::Info) << "Vanilla province duplication - " << newProvinceID << " already loaded! Preferring unique entry over mapping.";
				}
				else {
					auto newProvince = std::make_shared<Province>(newProvinceID, *provinces.find(baseProvinceID)->second);
					provinces.emplace(newProvinceID, newProvince);
				}
			}
		}
		catch (std::exception& e) {
			Log(LogLevel::Warning) << "Invalid province filename: " << ck3Path << "/game/history/province_mapping/" << fileName << " : " << e.what();
		}
	}


	LOG(LogLevel::Info) << ">> Loaded " << provinces.size() << " province definitions.";
}


void CK3::World::importImperatorProvinces(const Imperator::World& impWorld) {
	LOG(LogLevel::Info) << "-> Importing Imperator Provinces";
	auto counter = 0;
	// Imperator provinces map to a subset of CK3 provinces. We'll only rewrite those we are responsible for.
	for (const auto& [provinceID, province] : provinces) {
		const auto& impProvinces = provinceMapper.getImperatorProvinceNumbers(provinceID);
		// Provinces we're not affecting will not be in this list.
		if (impProvinces.empty())
			continue;
		// Next, we find what province to use as its initializing source.
		const auto& sourceProvince = determineProvinceSource(impProvinces, impWorld);
		if (!sourceProvince) {
			Log(LogLevel::Warning) << "Could not determine source province for CK3 province " << provinceID;
			continue; // MISMAP, or simply have mod provinces loaded we're not using.
		}
		else {
			province->initializeFromImperator(sourceProvince->second, cultureMapper, religionMapper);
		}
		// And finally, initialize it.
		++counter;
	}
	LOG(LogLevel::Info) << ">> " << impWorld.getProvinces().size() << " Imperator provinces imported into " << counter << " CK3 provinces.";
}


std::optional<std::pair<unsigned long long, std::shared_ptr<Imperator::Province>>> CK3::World::determineProvinceSource(const std::vector<unsigned long long>& impProvinceNumbers,
	const Imperator::World& impWorld) const
{
	// determine ownership by province development.
	std::map<unsigned long long, std::vector<std::shared_ptr<Imperator::Province>>> theClaims; // owner, offered province sources
	std::map<unsigned long long, int> theShares;														// owner, development
	std::optional<unsigned long long> winner;
	auto maxDev = -1;

	for (auto imperatorProvinceID : impProvinceNumbers) {
		const auto& impProvince = impWorld.getProvinces().find(imperatorProvinceID);
		if (impProvince == impWorld.getProvinces().end()) {
			Log(LogLevel::Warning) << "Source province " << imperatorProvinceID << " is not on the list of known provinces!";
			continue; // Broken mapping, or loaded a mod changing provinces without using it.
		}
		const auto ownerID = impProvince->second->getOwner().first;
		theClaims[ownerID].emplace_back(impProvince->second);
		theShares[ownerID] = lround(impProvince->second->getBuildingsCount() + impProvince->second->getPopCount());
	}
	// Let's see who the lucky winner is.
	for (const auto& [owner, development] : theShares) {
		if (development > maxDev) {
			winner = owner;
			maxDev = development;
		}
	}
	if (!winner) {
		return std::nullopt;
	}

	// Now that we have a winning owner, let's find its largest province to use as a source.
	maxDev = -1; // We can have winning provinces with weight = 0;

	std::pair<unsigned long long, std::shared_ptr<Imperator::Province>> toReturn;
	for (const auto& province : theClaims.at(*winner)) {
		const auto provinceWeight = province->getBuildingsCount() + province->getPopCount();

		if (static_cast<int>(provinceWeight) > maxDev) {
			toReturn.first = province->getID();
			toReturn.second = province;
			maxDev = provinceWeight;
		}
	}
	if (!toReturn.first || !toReturn.second) {
		return std::nullopt;
	}
	return toReturn;
}


void CK3::World::addHoldersAndHistoryToTitles(const Imperator::World& impWorld) {
	for (const auto& [name, title] : getTitles()) {
		if (name.starts_with("c_") && title->capitalBaronyProvince > 0) { // title is a county and its capital province has a valid ID (0 is not a valid province in CK3)
			if (!provinces.contains(title->capitalBaronyProvince))
				LOG(LogLevel::Warning) << "Capital barony province not found " << title->capitalBaronyProvince;
			else {
				auto& impProvince = provinces.find(title->capitalBaronyProvince)->second->getImperatorProvince();
				if (impProvince) {
					std::optional<unsigned long long> impMonarch;
					if (auto impCountry = impProvince->getOwner().second) {
						impMonarch = impCountry->getMonarch();
					}
					if (impMonarch) {
						title->holder = "imperator" + std::to_string(*impMonarch);
						countyHoldersCache.emplace(title->holder);
					}
				}
				else { // county is probably outside of Imperator map
					title->addHistory(landedTitles, titlesHistory);
					if (!title->holder.empty())
						countyHoldersCache.emplace(title->holder);
				}
			}
		}
		else if (!name.starts_with("c_") && !name.starts_with("b_") && !title->isImportedOrUpdatedFromImperator()) { // title is a duchy or higher, from vanilla
			// update title holder, liege and history
			title->addHistory(landedTitles, titlesHistory);
		}
	}
}


void CK3::World::removeInvalidLandlessTitles() {
	std::set<std::string> removedGeneratedTitles;
	std::set<std::string> revokedVanillaTitles;

	for (const auto& [name, title] : getTitles()) {
		//important check: if duchy/kingdom/empire title holder holds no county (is landless), remove the title
		// this also removes landless titles initialized from Imperator
		if (!name.starts_with("c_") && !name.starts_with("b_") && !countyHoldersCache.contains(title->holder)) {
			if (!getTitles().find(name)->second->landless) { // does not have landless attribute set to true
				if (title->isImportedOrUpdatedFromImperator() && name.find("IMPTOCK3") != std::string::npos) {
					removedGeneratedTitles.emplace(name);
					landedTitles.eraseTitle(name);
				}
				else {
					revokedVanillaTitles.emplace(name);
					title->holder = "0";
				}
			}
		}
	}
	if (!removedGeneratedTitles.empty()) {
		std::string msg = "Found landless generated titles that can't be landless: ";
		for (const auto& name : removedGeneratedTitles) {
			msg += name;
			msg += ", ";
		}
		msg.erase(msg.length() - 2); // remove last ", "
		Log(LogLevel::Debug) << msg;
	}
	if (!revokedVanillaTitles.empty()) {
		std::string msg = "Found landless vanilla titles that can't be landless: ";
		for (const auto& name : revokedVanillaTitles) {
			msg += name;
			msg += ", ";
		}
		msg.erase(msg.length() - 2); // remove last ", "
		Log(LogLevel::Debug) << msg;
	}
}


void CK3::World::linkSpouses() {
	auto counterSpouse = 0;
	for (const auto& [ck3CharacterID, ck3Character] : characters) {
		std::map<unsigned long long, std::shared_ptr<Character>> newSpouses;
		// make links between Imperator characters
		for (const auto& [impSpouseID, impSpouseCharacter] : ck3Character->imperatorCharacter->getSpouses()) {
			if (impSpouseCharacter != nullptr) {
				auto& ck3SpouseCharacter = impSpouseCharacter->getCK3Character();
				ck3Character->addSpouse(std::pair(ck3SpouseCharacter->ID, ck3SpouseCharacter));
				ck3SpouseCharacter->addSpouse(std::pair(ck3CharacterID, ck3Character));
				++counterSpouse;
			}
		}
	}
	Log(LogLevel::Info) << "<> " << counterSpouse << " spouses linked in CK3.";
}


void CK3::World::linkMothersAndFathers() {
	auto counterMother = 0;
	auto counterFather = 0;
	for (const auto& [ck3CharacterID, ck3Character] : characters) {
		// make links between Imperator characters
		const auto& [impMotherID, impMotherCharacter] = ck3Character->imperatorCharacter->getMother();
		if (impMotherCharacter != nullptr) {
			auto& ck3MotherCharacter = impMotherCharacter->getCK3Character();
			ck3Character->setMother(std::pair(ck3MotherCharacter->ID, ck3MotherCharacter));
			ck3MotherCharacter->addChild(std::pair(ck3CharacterID, ck3Character));
			++counterMother;
		}

		// make links between Imperator characters
		const auto& [impFatherID, impFatherCharacter] = ck3Character->imperatorCharacter->getFather();
		if (impFatherCharacter != nullptr) {
			auto& ck3FatherCharacter = impFatherCharacter->getCK3Character();
			ck3Character->setFather(std::pair(ck3FatherCharacter->ID, ck3FatherCharacter));
			ck3FatherCharacter->addChild(std::pair(ck3CharacterID, ck3Character));
			++counterFather;
		}
	}
	Log(LogLevel::Info) << "<> " << counterMother << " mothers and " << counterFather << " fathers linked in CK3.";
}


void CK3::World::importImperatorFamilies(const Imperator::World& impWorld) {
	LOG(LogLevel::Info) << "-> Importing Imperator Families";

	// dynasties only holds dynasties converted from Imperator families, as vanilla ones aren't modified
	for (const auto& [_, family] : impWorld.getFamilies()) {
		if (family->isMinor())
			continue;

		auto newDynasty = std::make_shared<Dynasty>(*family, localizationMapper);
		dynasties.emplace(newDynasty->getID(), newDynasty);
	}
	LOG(LogLevel::Info) << ">> " << dynasties.size() << " total families imported.";
}
