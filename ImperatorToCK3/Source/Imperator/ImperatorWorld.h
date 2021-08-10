#ifndef IMPERATOR_WORLD_H
#define IMPERATOR_WORLD_H


#include <set>
#include <string>
#include "Characters/Characters.h"
#include "ConverterVersion.h"
#include "Countries/Countries.h"
#include "Date.h"
#include "Families/Families.h"
#include "GameVersion.h"
#include "Genes/GenesDB.h"
#include "ModLoader/ModLoader.h"
#include "Parser.h"
#include "Provinces/Pops.h"
#include "Provinces/Provinces.h"



class Configuration;


namespace Imperator {

class World: commonItems::parser {
  public:
	explicit World(const Configuration& theConfiguration, const commonItems::ConverterVersion& converterVersion);

	[[nodiscard]] const auto& getEndDate() const { return endDate; }
	[[nodiscard]] const auto& getMods() const { return mods; }
	[[nodiscard]] const auto& getFamilies() const { return families.getFamilies(); }
	[[nodiscard]] const auto& getCharacters() const { return characters.getCharacters(); }
	[[nodiscard]] const auto& getProvinces() const { return provinces.getProvinces(); }
	[[nodiscard]] const auto& getCountries() const { return countries.getCountries(); }

  private:
	void verifySave(const std::string& saveGamePath);

	void processDebugModeSave(const std::string& saveGamePath);
	void processCompressedEncodedSave(const std::string& saveGamePath);
	void processSave(const std::string& saveGamePath);

	void parseGenes(const Configuration& theConfiguration);

	date startDate = date("450.10.1", true);
	date endDate = date("727.2.17", true);
	GameVersion ImperatorVersion;
	std::set<std::string> DLCs;

	enum class SaveType { INVALID = 0, PLAINTEXT = 1, COMPRESSED_ENCODED = 2 };
	struct saveData {
		SaveType saveType = SaveType::INVALID;
		int zipStart = 0;
		std::string gameState;
	};
	saveData saveGame;

	GenesDB genes;
	Families families;
	Characters characters;
	Pops pops;
	Provinces provinces;
	Countries countries;

	Mods mods;
};

}  // namespace Imperator



#endif	// IMPERATOR_WORLD_H
