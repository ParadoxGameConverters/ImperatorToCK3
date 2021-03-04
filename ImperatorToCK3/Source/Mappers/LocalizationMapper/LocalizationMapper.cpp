#include "LocalizationMapper.h"
#include "Configuration/Configuration.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include <fstream>
#include <set>



void mappers::LocalizationMapper::scrapeLocalizations(const Configuration& theConfiguration, const std::map<std::string, std::string>& mods) {
	LOG(LogLevel::Info) << "-> Reading Words";

	scrapeLanguage("english", theConfiguration.getImperatorPath() + "/game/localization");
	scrapeLanguage("french", theConfiguration.getImperatorPath() + "/game/localization");
	scrapeLanguage("german", theConfiguration.getImperatorPath() + "/game/localization");
	scrapeLanguage("russian", theConfiguration.getImperatorPath() + "/game/localization");
	scrapeLanguage("spanish", theConfiguration.getImperatorPath() + "/game/localization");

	for (const auto& mod : mods) {
		if (commonItems::DoesFolderExist(mod.second + "/localization")) {
			Log(LogLevel::Info) << "\t>> Found some words in: " << mod.second + "/localization";
			scrapeLanguage("english", mod.second + "/localization");
			scrapeLanguage("french", mod.second + "/localization");
			scrapeLanguage("german", mod.second + "/localization");
			scrapeLanguage("russian", mod.second + "/localization");
			scrapeLanguage("spanish", mod.second + "/localization");
		}
	}

	LOG(LogLevel::Info) << ">> " << localizations.size() << " words read.";
}


void mappers::LocalizationMapper::scrapeLanguage(const std::string& language, const std::string& path) {
	if (!commonItems::DoesFolderExist(path + "/" + language))
		return;
	auto filenames = commonItems::GetAllFilesInFolderRecursive(path + "/" + language);
	for (const auto& file : filenames) {
		std::ifstream fileStream(path + "/" + language + "/" + file);
		if (fileStream.is_open())
			scrapeStream(fileStream, language);
		fileStream.close();
	}
}


void mappers::LocalizationMapper::scrapeStream(std::istream& theStream, const std::string& language) {
	while (!theStream.eof()) {
		std::string line;
		getline(theStream, line);

		if (line.length() < 4 || line.at(0) == '#' || line.at(1) == '#')
			continue;

		const auto sepLoc = line.find_first_of(':');
		if (sepLoc == std::string::npos)
			continue;
		const auto key = line.substr(1, sepLoc-1);
		const auto newLine = line.substr(sepLoc + 1, line.length());
		const auto quoteLoc = newLine.find_first_of('\"');
		const auto quote2Loc = newLine.find_last_of('\"');
		if (quoteLoc == std::string::npos || quote2Loc == std::string::npos || quote2Loc - quoteLoc == 0)
			continue;
		const auto value = newLine.substr(quoteLoc + 1, quote2Loc - quoteLoc - 1);

		if (auto locItr = localizations.find(key); locItr != localizations.end()) {
			if (language == "english")
				locItr->second.english = value;
			if (language == "french")
				locItr->second.french = value;
			if (language == "german")
				locItr->second.german = value;
			if (language == "russian")
				locItr->second.russian = value;
			if (language == "spanish")
				locItr->second.spanish = value;
		}
		else {
			LocBlock newBlock;
			if (language == "english")
				newBlock.english = value;
			if (language == "french")
				newBlock.french = value;
			if (language == "german")
				newBlock.german = value;
			if (language == "russian")
				newBlock.russian = value;
			if (language == "spanish")
				newBlock.spanish = value;
			localizations.emplace(key, newBlock);
		}
	}
}


std::optional<mappers::LocBlock> mappers::LocalizationMapper::getLocBlockForKey(const std::string& key) const {
	const auto& keyItr = localizations.find(key);
	if (keyItr == localizations.end())
		return std::nullopt;

	if (!keyItr->second.english.empty() && (keyItr->second.spanish.empty() || keyItr->second.russian.empty() || keyItr->second.german.empty() || keyItr->second.french.empty())) {
		auto newBlock = keyItr->second;
		if (newBlock.spanish.empty())
			newBlock.spanish = newBlock.english;
		if (newBlock.russian.empty())
			newBlock.russian = newBlock.english;
		if (newBlock.german.empty())
			newBlock.german = newBlock.english;
		if (newBlock.french.empty())
			newBlock.french = newBlock.english;
		return newBlock;
	}
	// either all is well, or we're missing english. Can't do anything about the latter.
	return keyItr->second;
}
