#include "ImperatorWorld.h"
#include "../Common/CommonFunctions.h"
#include "../Common/Version.h"
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
	registerRegex("\\bSAV\\w*\\b", [](const std::string& unused, std::istream& theStream) {});
	registerKeyword("version", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString versionString(theStream);
		ImperatorVersion = Version(versionString.getString());
		Log(LogLevel::Info) << "<> Savegame version: " << versionString.getString();
	});
	registerKeyword("date", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString dateString(theStream);
		endDate = date(dateString.getString());
		Log(LogLevel::Info) << "<> Date: " << dateString.getString();
	});

	registerRegex("[A-Za-z0-9\\_]+", commonItems::ignoreItem);

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
		if (name == "meta") {
			LOG(LogLevel::Info) << ">> Uncompressing metadata";
			saveGame.metadata = std::string{ std::istreambuf_iterator<char>(*entry->GetDecompressionStream()), std::istreambuf_iterator<char>() };
		}
		else if (name == trimPath(saveGamePath)) {
			LOG(LogLevel::Info) << ">> Uncompressing gamestate";
			saveGame.gamestate = std::string{ std::istreambuf_iterator<char>(*entry->GetDecompressionStream()), std::istreambuf_iterator<char>() };
		}
		else
			throw std::runtime_error("Unrecognized savegame structure!");
	}
	return true;
}