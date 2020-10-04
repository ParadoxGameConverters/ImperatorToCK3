#include "ImperatorWorld.h"
#include "CommonFunctions.h"
#include "GameVersion.h"
#include "../Configuration/Configuration.h"
#include "../Helpers/rakaly_wrapper.h"
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
	
	parseGenes(theConfiguration);
	
	//parse the save
	registerRegex(R"(\bSAV\w*\b)", [](const std::string& unused, std::istream& theStream) {});
	registerKeyword("version", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString versionString(theStream);
		ImperatorVersion = GameVersion(versionString.getString());
		Log(LogLevel::Info) << "<> Savegame version: " << versionString.getString();
	});
	registerKeyword("date", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString dateString(theStream);
		endDate = date(dateString.getString(), true); // converted to AD
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
		characters = CharactersBloc(theStream, genes, endDate).getCharactersFromBloc();
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
	processSave(theConfiguration.getSaveGamePath());

	auto gameState = std::istringstream(saveGame.gameState);
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

void ImperatorWorld::World::processSave(const std::string& saveGamePath)
{
	switch (saveGame.saveType)
	{
	case SaveType::PLAINTEXT:
		LOG(LogLevel::Info) << "-> Importing debug_mode Imperator save.";
		processDebugModeSave(saveGamePath);
		break;
	case SaveType::COMPRESSED_ENCODED:
		LOG(LogLevel::Info) << "-> Importing regular Imperator save.";
		processCompressedEncodedSave(saveGamePath);
		break;
	case SaveType::INVALID:
		throw std::runtime_error("Unknown save type.");
	}
}

void ImperatorWorld::World::verifySave(const std::string& saveGamePath)
{
	std::ifstream saveFile(fs::u8path(saveGamePath), std::ios::binary);
	if (!saveFile.is_open())
		throw std::runtime_error("Could not open save! Exiting!");

	char buffer[10];
	saveFile.get(buffer, 4);
	if (buffer[0] != 'S' || buffer[1] != 'A' || buffer[2] != 'V')
		throw std::runtime_error("Savefile of unknown type.");

	char ch;
	do
	{ // skip until newline
		ch = static_cast<char>(saveFile.get());
	} while (ch != '\n' && ch != '\r');

	saveFile.seekg(0, std::ios::end);
	const auto length = saveFile.tellg();
	if (length < 65536)
	{
		throw std::runtime_error("Savegame seems a bit too small.");
	}
	saveFile.seekg(0, std::ios::beg);
	auto* const bigBuf = new char[65536];
	saveFile.read(bigBuf, 65536);
	if (saveFile.gcount() < 65536)
		throw std::runtime_error("Read only: " + std::to_string(saveFile.gcount()));

	saveGame.saveType = SaveType::PLAINTEXT;
	for (int i = 0; i < 65533; ++i)
		if (*reinterpret_cast<uint32_t*>(bigBuf + i) == 0x04034B50 && *reinterpret_cast<uint16_t*>(bigBuf + i - 2) == 4)
		{
			saveGame.zipStart = i;
			saveGame.saveType = SaveType::COMPRESSED_ENCODED;
			break;
		}

	delete[] bigBuf;
}

void ImperatorWorld::World::processDebugModeSave(const std::string& saveGamePath)
{
	const std::ifstream inBinary(fs::u8path(saveGamePath), std::ios::binary);
	std::stringstream inStream;
	inStream << inBinary.rdbuf();
	saveGame.gameState = inStream.str();
}

void ImperatorWorld::World::processCompressedEncodedSave(const std::string& saveGamePath)
{
	const std::ifstream saveFile(fs::u8path(saveGamePath), std::ios::binary);
	std::stringstream inStream;
	inStream << saveFile.rdbuf();
	const std::string inBinary(std::istreambuf_iterator<char>(inStream), {});

	saveGame.gameState = rakaly::meltImperator(inBinary);
}


void ImperatorWorld::World::parseGenes(const Configuration& theConfiguration)
{
	genes = GenesDB(theConfiguration.getImperatorPath() + "/game/common/genes/00_genes.txt");
}