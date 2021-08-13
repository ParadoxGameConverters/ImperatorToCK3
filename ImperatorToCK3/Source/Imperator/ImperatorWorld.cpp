#include "ImperatorWorld.h"
#include <ZipFile.h>
#include <filesystem>
#include <fstream>
#include "CommonRegexes.h"
#include "Configuration/Configuration.h"
#include "Date.h"
#include "GameVersion.h"
#include "Helpers/rakaly_wrapper.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include "ParserHelpers.h"



namespace fs = std::filesystem;

Imperator::World::World(const Configuration& theConfiguration, const commonItems::ConverterVersion& converterVersion) {
	Log(LogLevel::Info) << "*** Hello Imperator, Roma Invicta! ***";

	parseGenes(theConfiguration);

	// parse the save
	registerRegex(R"(\bSAV\w*\b)", [](const std::string& unused, std::istream& theStream) {});
	registerKeyword("version", [this, converterVersion](std::istream& theStream) {
		const auto versionString = commonItems::getString(theStream);
		ImperatorVersion = GameVersion(versionString);
		Log(LogLevel::Info) << "<> Savegame version: " << versionString;

		if (converterVersion.getMinSource() > ImperatorVersion) {
			Log(LogLevel::Error) << "Converter requires a minimum save from v" << converterVersion.getMinSource().toShortString();
			throw std::runtime_error("Savegame vs converter version mismatch!");
		}
		if (!converterVersion.getMaxSource().isLargerishThan(ImperatorVersion)) {
			Log(LogLevel::Error) << "Converter requires a maximum save from v" << converterVersion.getMaxSource().toShortString();
			throw std::runtime_error("Savegame vs converter version mismatch!");
		}
	});
	registerKeyword("date", [this](std::istream& theStream) {
		const auto dateString = commonItems::getString(theStream);
		endDate = date(dateString, true);  // converted to AD
		Log(LogLevel::Info) << "<> Date: " << dateString;
	});
	registerKeyword("enabled_dlcs", [this](std::istream& theStream) {
		const auto& theDLCs = commonItems::getStrings(theStream);
		DLCs.insert(theDLCs.begin(), theDLCs.end());
		for (const auto& dlc : DLCs) {
			Log(LogLevel::Info) << "<> Enabled DLC: " << dlc;
		}
	});
	registerKeyword("enabled_mods", [&](std::istream& theStream) {
		Log(LogLevel::Info) << "-> Detecting used mods.";
		const auto modsList = commonItems::getStrings(theStream);
		Log(LogLevel::Info) << "<> Savegame claims " << modsList.size() << " mods used:";
		Mods incomingMods;
		for (const auto& modPath : modsList) {
			Log(LogLevel::Info) << "Used mod: " << modPath;
			incomingMods.emplace_back("", modPath);
		}

		// Let's locate, verify and potentially update those mods immediately.
		commonItems::ModLoader modLoader;
		modLoader.loadMods(theConfiguration.getImperatorDocsPath(), incomingMods);
		mods = modLoader.getMods();
	});
	registerKeyword("family", [this](std::istream& theStream) {
		Log(LogLevel::Info) << "-> Loading Families";
		families = FamiliesBloc(theStream).getFamiliesFromBloc();
		Log(LogLevel::Info) << ">> Loaded " << families.getFamilies().size() << " families.";
	});
	registerKeyword("character", [this](std::istream& theStream) {
		Log(LogLevel::Info) << "-> Loading Characters";
		characters = CharactersBloc(theStream, genes).getCharactersFromBloc();
		Log(LogLevel::Info) << ">> Loaded " << characters.getCharacters().size() << " characters.";
	});
	registerKeyword("provinces", [this](std::istream& theStream) {
		Log(LogLevel::Info) << "-> Loading Provinces";
		provinces = Provinces(theStream);
		Log(LogLevel::Info) << ">> Loaded " << provinces.getProvinces().size() << " provinces.";
	});
	registerKeyword("country", [this](std::istream& theStream) {
		Log(LogLevel::Info) << "-> Loading Countries";
		countries = CountriesBloc(theStream).getCountriesFromBloc();
		Log(LogLevel::Info) << ">> Loaded " << countries.getCountries().size() << " countries.";
	});
	registerKeyword("population", [this](std::istream& theStream) {
		Log(LogLevel::Info) << "-> Loading Pops";
		pops = PopsBloc(theStream).getPopsFromBloc();
		Log(LogLevel::Info) << ">> Loaded " << pops.getPops().size() << " pops.";
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);

	Log(LogLevel::Info) << "-> Verifying Imperator save.";
	verifySave(theConfiguration.getSaveGamePath());
	processSave(theConfiguration.getSaveGamePath());

	auto gameState = std::istringstream(saveGame.gameState);
	parseStream(gameState);
	clearRegisteredKeywords();


	Log(LogLevel::Info) << "*** Building World ***";

	// Link all the intertwining pointers
	Log(LogLevel::Info) << "-- Linking Characters with Families";
	characters.linkFamilies(families);
	families.removeUnlinkedMembers();
	Log(LogLevel::Info) << "-- Linking Characters with Spouses";
	characters.linkSpouses();
	Log(LogLevel::Info) << "-- Linking Characters with Mothers and Fathers";
	characters.linkMothersAndFathers();
	Log(LogLevel::Info) << "-- Linking Provinces with Pops";
	provinces.linkPops(pops);
	Log(LogLevel::Info) << "-- Linking Provinces with Countries";
	provinces.linkCountries(countries);
	Log(LogLevel::Info) << "-- Linking Countries with Families";
	countries.linkFamilies(families);

	Log(LogLevel::Info) << "*** Good-bye Imperator, rest in peace. ***";
}


void Imperator::World::processSave(const std::string& saveGamePath) {
	switch (saveGame.saveType) {
		case SaveType::PLAINTEXT:
			Log(LogLevel::Info) << "-> Importing debug_mode Imperator save.";
			processDebugModeSave(saveGamePath);
			break;
		case SaveType::COMPRESSED_ENCODED:
			Log(LogLevel::Info) << "-> Importing regular Imperator save.";
			processCompressedEncodedSave(saveGamePath);
			break;
		case SaveType::INVALID:
			throw std::runtime_error("Unknown save type.");
	}
}


void Imperator::World::verifySave(const std::string& saveGamePath) {
	std::ifstream saveFile(fs::u8path(saveGamePath), std::ios::binary);
	if (!saveFile.is_open())
		throw std::runtime_error("Could not open save! Exiting!");

	char buffer[10];
	saveFile.get(buffer, 4);
	if (buffer[0] != 'S' || buffer[1] != 'A' || buffer[2] != 'V')
		throw std::runtime_error("Savefile of unknown type.");

	char ch;
	do {  // skip until newline
		ch = static_cast<char>(saveFile.get());
	} while (ch != '\n' && ch != '\r');

	saveFile.seekg(0, std::ios::end);
	const auto length = saveFile.tellg();
	if (length < 65536) {
		throw std::runtime_error("Savegame seems a bit too small.");
	}
	saveFile.seekg(0, std::ios::beg);
	auto* const bigBuf = new char[65536];
	saveFile.read(bigBuf, 65536);
	if (saveFile.gcount() < 65536)
		throw std::runtime_error("Read only: " + std::to_string(saveFile.gcount()));

	saveGame.saveType = SaveType::PLAINTEXT;
	for (auto i = 0; i < 65533; ++i)
		if (*reinterpret_cast<uint32_t*>(bigBuf + i) == 0x04034B50 && *reinterpret_cast<uint16_t*>(bigBuf + i - 2) == 4) {
			saveGame.zipStart = i;
			saveGame.saveType = SaveType::COMPRESSED_ENCODED;
			break;
		}

	delete[] bigBuf;
}


void Imperator::World::processDebugModeSave(const std::string& saveGamePath) {
	const std::ifstream inBinary(fs::u8path(saveGamePath), std::ios::binary);
	std::stringstream inStream;
	inStream << inBinary.rdbuf();
	saveGame.gameState = inStream.str();
}


void Imperator::World::processCompressedEncodedSave(const std::string& saveGamePath) {
	const std::ifstream saveFile(fs::u8path(saveGamePath), std::ios::binary);
	std::stringstream inStream;
	inStream << saveFile.rdbuf();
	const std::string inBinary(std::istreambuf_iterator<char>(inStream), {});
	saveGame.gameState = rakaly::meltImperator(inBinary);
}


void Imperator::World::parseGenes(const Configuration& theConfiguration) {
	genes = GenesDB(theConfiguration.getImperatorPath() + "/game/common/genes/00_genes.txt");
}
