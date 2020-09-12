#include "LocalizationMapper.h"
#include "../../Configuration/Configuration.h"
#include "Log.h"
#include "OSCompatibilityLayer.h"
#include <fstream>
#include <set>

void mappers::LocalizationMapper::scrapeLocalizations(const Configuration& theConfiguration)
{
	LOG(LogLevel::Info) << "-> Reading Words";
	for (const auto& language : std::set<std::string>{ "english", "french", "german", "russian", "spanish" })
	{
		auto filenames = Utils::GetAllFilesInFolder(theConfiguration.getImperatorPath() + "/game/localization/" + language + "/");
		for (const auto& file : filenames)
		{
			std::ifstream theFile(theConfiguration.getImperatorPath() + "/game/localization/" + language + "/" + file);
			if (language == "english")
				scrapeStream(theFile, langEnum::ENGLISH);
			else if (language == "french")
				scrapeStream(theFile, langEnum::FRENCH);
			else if (language == "german")
				scrapeStream(theFile, langEnum::GERMAN);
			else if (language == "russian")
				scrapeStream(theFile, langEnum::RUSSIAN);
			else if (language == "spanish")
				scrapeStream(theFile, langEnum::SPANISH);
			theFile.close();
		}
	}
	// Override with our keys
	if (Utils::DoesFileExist("configurables/english_imp_localization_override.yml"))
	{
		std::ifstream theFile("configurables/english_imp_localization_override.yml");
		scrapeStream(theFile, langEnum::ENGLISH);
		theFile.close();
	}
	if (Utils::DoesFileExist("configurables/french_imp_localization_override.yml"))
	{
		std::ifstream theFile("configurables/french_imp_localization_override.yml");
		scrapeStream(theFile, langEnum::FRENCH);
		theFile.close();
	}
	if (Utils::DoesFileExist("configurables/german_imp_localization_override.yml"))
	{
		std::ifstream theFile("configurables/german_imp_localization_override.yml");
		scrapeStream(theFile, langEnum::GERMAN);
		theFile.close();
	}
	if (Utils::DoesFileExist("configurables/russian_imp_localization_override.yml"))
	{
		std::ifstream theFile("configurables/russian_imp_localization_override.yml");
		scrapeStream(theFile, langEnum::RUSSIAN);
		theFile.close();
	}
	if (Utils::DoesFileExist("configurables/spanish_imp_localization_override.yml"))
	{
		std::ifstream theFile("configurables/spanish_imp_localization_override.yml");
		scrapeStream(theFile, langEnum::SPANISH);
		theFile.close();
	}
		
	LOG(LogLevel::Info) << ">> " << localizationsEnglish.size()+localizationsFrench.size()+localizationsGerman.size()+localizationsRussian.size()+localizationsSpanish.size() << " words read.";
}

void mappers::LocalizationMapper::scrapeStream(std::istream& theStream, const langEnum language)
{
	while (!theStream.eof())
	{
		std::string line;
		getline(theStream, line);

		if (line[0] == '#' || line[0] == ':' || line.find(':') == std::string::npos || line.find_first_of("l_") == 0)
			continue;

		const auto sepLocFirst = line.find_first_of(':');
		auto key = line.substr(1, sepLocFirst-1);
		const auto sepLocLast = line.find_first_of(' ', sepLocFirst+1);
		auto loc = line.substr(sepLocLast+2, line.size()-sepLocLast-3); // gets the loc string (without quotes)

		switch (language)
		{
		case langEnum::ENGLISH:
		{
			if (localizationsEnglish.count(key))
				localizationsEnglish[key] = loc;
			else
				localizationsEnglish.insert(std::pair(key, loc));
			break;
		}
		case langEnum::FRENCH:
		{
			if (localizationsFrench.count(key))
				localizationsFrench[key] = loc;
			else
				localizationsFrench.insert(std::pair(key, loc));
			break;
		}
		case langEnum::GERMAN:
		{
			if (localizationsGerman.count(key))
				localizationsGerman[key] = loc;
			else
				localizationsGerman.insert(std::pair(key, loc));
			break;
		}
		case langEnum::RUSSIAN:
		{
			if (localizationsRussian.count(key))
				localizationsRussian[key] = loc;
			else
				localizationsRussian.insert(std::pair(key, loc));
			break;
		}
		case langEnum::SPANISH:
		{
			if (localizationsSpanish.count(key))
				localizationsSpanish[key] = loc;
			else
				localizationsSpanish.insert(std::pair(key, loc));
			break;
		}
		default:
			break;
		}
	}
}


std::optional<std::string> mappers::LocalizationMapper::getLocBlockForKey(const std::string& key, const langEnum language) const
{
	switch (language)
	{
	case langEnum::ENGLISH:
	{
		const auto& keyItr = localizationsEnglish.find(key);
		if (keyItr == localizationsEnglish.end())
			return std::nullopt;
		return keyItr->second;
	}
	case langEnum::FRENCH:
	{
		const auto& keyItr = localizationsFrench.find(key);
		if (keyItr == localizationsFrench.end()) // if key not found, try english as fallback
			return getLocBlockForKey(key, langEnum::ENGLISH);
		return keyItr->second;
	}
	case langEnum::GERMAN:
	{
		const auto& keyItr = localizationsGerman.find(key);
		if (keyItr == localizationsGerman.end()) // if key not found, try english as fallback
			return getLocBlockForKey(key, langEnum::ENGLISH);
		return keyItr->second;
	}
	case langEnum::RUSSIAN:
	{
		const auto& keyItr = localizationsRussian.find(key);
		if (keyItr == localizationsRussian.end()) // if key not found, try english as fallback
			return getLocBlockForKey(key, langEnum::ENGLISH);
		return keyItr->second;
	}
	case langEnum::SPANISH:
	{
		const auto& keyItr = localizationsSpanish.find(key);
		if (keyItr == localizationsSpanish.end()) // if key not found, try english as fallback
			return getLocBlockForKey(key, langEnum::ENGLISH);
		return keyItr->second;
	}
	default:
		return getLocBlockForKey(key, langEnum::ENGLISH);
	}
}