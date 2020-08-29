#include "ImperatorWorld.h"
#include "CommonFunctions.h"
#include "GameVersion.h"
#include "../Configuration/Configuration.h"
#include "Date.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include "ParserHelpers.h"
#include <ZipFile.h>
#include <filesystem>
#include <fstream>

namespace fs = std::filesystem;

ImperatorWorld::World::World(const Configuration& theConfiguration)
{
	LOG(LogLevel::Info) << "*** Hello Imperator, Roma Invicta! ***";
	
	// parse the genes file
	genes = GenesDB(theConfiguration.getImperatorPath() + "/game/common/genes/00_genes.txt");

	//parse the save
	registerRegex(R"(\bSAV\w*\b)", [](const std::string& unused, std::istream& theStream) {});
	registerKeyword("version", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString versionString(theStream);
		ImperatorVersion = GameVersion(versionString.getString());
		Log(LogLevel::Info) << "<> Savegame version: " << versionString.getString();
	});
	registerKeyword("date", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString dateString(theStream);
		endDate = date(dateString.getString());
		Log(LogLevel::Info) << "<> Date: " << dateString.getString();
	});
	/*registerKeyword("enabled_dlcs", [this](const std::string& unused, std::istream& theStream) {	/// not really needed at the moment of writing, uncomment when needed 
		const commonItems::stringList dlcsList(theStream);
		const auto& theDLCs = dlcsList.getStrings();
		DLCs.insert(theDLCs.begin(), theDLCs.end());
		for (const auto& dlc : DLCs) LOG(LogLevel::Info) << "<> Enabled DLC: " << dlc;
	}); */
	registerKeyword("enabled_mods", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::stringList modsList(theStream);
		const auto& theMods = modsList.getStrings();
		Mods.insert(theMods.begin(), theMods.end());
		for (const auto& mod : Mods) LOG(LogLevel::Info) << "<> Enabled mod: " << mod;
	});
	registerKeyword("family", [this](const std::string& unused, std::istream& theStream) {
		LOG(LogLevel::Info) << "-> Loading Families";
		families = FamiliesBloc(theStream).getFamiliesFromBloc();
		LOG(LogLevel::Info) << ">> Loaded " << families.getFamilies().size() << " families.";
	});
	
	registerKeyword("character", [this](const std::string& unused, std::istream& theStream) {
		LOG(LogLevel::Info) << "-> Loading Characters";
		characters = CharactersBloc(theStream).getCharactersFromBloc();
		LOG(LogLevel::Info) << ">> Loaded " << characters.getCharacters().size() << " characters.";
	});

	registerKeyword("provinces", [this](const std::string& unused, std::istream& theStream) {
		LOG(LogLevel::Info) << "-> Loading Provinces";
		provinces = Provinces(theStream);
		LOG(LogLevel::Info) << ">> Loaded " << provinces.getProvinces().size() << " provinces.";
	});

	registerKeyword("country", [this](const std::string& unused, std::istream& theStream) {
		LOG(LogLevel::Info) << "-> Loading Countries";
		countries = CountriesBloc(theStream).getCountriesFromBloc();
		LOG(LogLevel::Info) << ">> Loaded " << countries.getCountries().size() << " countries.   ";
	});

	registerKeyword("population", [this](const std::string& unused, std::istream& theStream) {
		LOG(LogLevel::Info) << "-> Loading Pops";
		pops = PopsBloc(theStream).getPopsFromBloc();
		LOG(LogLevel::Info) << ">> Loaded " << pops.getPops().size() << " pops.";
	});

	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);

	LOG(LogLevel::Info) << "-> Verifying Imperator save.";
	verifySave(theConfiguration.getSaveGamePath());
	
	LOG(LogLevel::Info) << "-> Importing Imperator save.";
	if (!saveGame.compressed) {
		std::ifstream inBinary(fs::u8path(theConfiguration.getSaveGamePath()), std::ios::binary);
		if (!inBinary.is_open()) {
			LOG(LogLevel::Error) << "Could not open " << theConfiguration.getSaveGamePath() << " for parsing.";
			throw std::runtime_error("Could not open " + theConfiguration.getSaveGamePath() + " for parsing.");
		}
		std::stringstream inStream;
		inStream << inBinary.rdbuf();
		saveGame.gamestate = inStream.str();
	}

	auto gameState = std::istringstream(saveGame.gamestate);
	parseStream(gameState);
	clearRegisteredKeywords();


	LOG(LogLevel::Info) << "*** Building World ***";

	// Link all the intertwining pointers
	LOG(LogLevel::Info) << "-- Linking Characters with Families";
	characters.linkFamilies(families);
	LOG(LogLevel::Info) << "-- Linking Characters with Spouses";
	characters.linkSpouses();
	LOG(LogLevel::Info) << "-- Linking Characters with Mothers and Fathers";
	characters.linkMothersAndFathers();
	LOG(LogLevel::Info) << "-- Linking Provinces with Pops";
	provinces.linkPops(pops);
	LOG(LogLevel::Info) << "-- Linking Countries with Families";
	countries.linkFamilies(families);

	LOG(LogLevel::Info) << "*** Good-bye Imperator, rest in peace. ***";
}

void ImperatorWorld::World::verifySave(const std::string& saveGamePath)
{
	std::ifstream saveFile(fs::u8path(saveGamePath));
	if (!saveFile.is_open()) throw std::runtime_error("Could not open save! Exiting!");

	char buffer[3];
	saveFile.get(buffer, 3);
	if (buffer[0] == 'P' && buffer[1] == 'K') {
		if (!uncompressSave(saveGamePath)) throw std::runtime_error("Failed to unpack the compressed save!");
		saveGame.compressed = true;
	}
	saveFile.close();
}

bool ImperatorWorld::World::uncompressSave(const std::string& saveGamePath)
{
	auto savefile = ZipFile::Open(saveGamePath);
	if (!savefile) return false;
	for (size_t entryNum = 0; entryNum < savefile->GetEntriesCount(); ++entryNum) {
		const auto& entry = savefile->GetEntry(entryNum);
		const auto& name = entry->GetName();
		if (name == trimPath(saveGamePath)) {
			LOG(LogLevel::Info) << ">> Uncompressing gamestate";
			saveGame.gamestate = std::string{ std::istreambuf_iterator<char>(*entry->GetDecompressionStream()), std::istreambuf_iterator<char>() };
		}
		else
			throw std::runtime_error("Unrecognized savegame structure!");
	}
	return true;
}