#ifndef CK3_WORLD_H
#define CK3_WORLD_H



#include "Character/CK3Character.h"
#include "ConverterVersion.h"
#include "Dynasties/Dynasty.h"
#include "Mappers/CoaMapper/CoaMapper.h"
#include "Mappers/CultureMapper/CultureMapper.h"
#include "Mappers/DeathReasonMapper/DeathReasonMapper.h"
#include "Mappers/GovernmentMapper/GovernmentMapper.h"
#include "Mappers/LocalizationMapper/LocalizationMapper.h"
#include "Mappers/NicknameMapper/NicknameMapper.h"
#include "Mappers/ProvinceMapper/ProvinceMapper.h"
#include "Mappers/RegionMapper/CK3RegionMapper.h"
#include "Mappers/RegionMapper/ImperatorRegionMapper.h"
#include "Mappers/ReligionMapper/ReligionMapper.h"
#include "Mappers/SuccessionLawMapper/SuccessionLawMapper.h"
#include "Mappers/TagTitleMapper/TagTitleMapper.h"
#include "Mappers/TraitMapper/TraitMapper.h"
#include "Province/CK3Province.h"
#include "Titles/LandedTitles.h"
#include "Titles/TitlesHistory.h"


class Configuration;

namespace Imperator {
class World;
}

namespace CK3 {

class World {
  public:
	World(const Imperator::World& impWorld, const Configuration& theConfiguration, const commonItems::ConverterVersion& converterVersion);

	[[nodiscard]] const auto& getCharacters() const { return characters; }
	[[nodiscard]] const auto& getDynasties() const { return dynasties; }
	[[nodiscard]] const auto& getTitles() const { return landedTitles.getTitles(); }
	[[nodiscard]] const auto& getProvinces() const { return provinces; }

  private:
	void importImperatorCharacters(const Imperator::World& impWorld, bool ConvertBirthAndDeathDates, date endDate);
	void importImperatorCharacter(const std::pair<unsigned long long, std::shared_ptr<Imperator::Character>>& character,
								  bool ConvertBirthAndDeathDates,
								  date endDate);
	void linkSpouses();
	void linkMothersAndFathers();

	void importImperatorFamilies(const Imperator::World& impWorld);

	void importImperatorCountries(const std::map<unsigned long long, std::shared_ptr<Imperator::Country>>& imperatorCountries);
	void importImperatorCountry(const std::pair<unsigned long long, std::shared_ptr<Imperator::Country>>& country,
								const std::map<unsigned long long, std::shared_ptr<Imperator::Country>>& imperatorCountries);

	void importVanillaProvinces(const std::string& ck3Path);
	void importImperatorProvinces(const Imperator::World& impWorld);

	void addHistoryToVanillaTitles();
	void overWriteCountiesHistory();
	void removeInvalidLandlessTitles();

	void purgeLandlessVanillaCharacters();

	[[nodiscard]] std::optional<std::pair<unsigned long long, std::shared_ptr<Imperator::Province>>> determineProvinceSource(
		const std::vector<unsigned long long>& impProvinceNumbers,
		const Imperator::World& impWorld) const;


	std::map<std::string, std::shared_ptr<Character>> characters;
	std::map<std::string, std::shared_ptr<Dynasty>> dynasties;
	LandedTitles landedTitles;
	std::map<unsigned long long, std::shared_ptr<Province>> provinces;

	mappers::CoaMapper coaMapper;
	mappers::CultureMapper cultureMapper;
	mappers::DeathReasonMapper deathReasonMapper;
	mappers::GovernmentMapper governmentMapper;
	mappers::LocalizationMapper localizationMapper;
	mappers::NicknameMapper nicknameMapper;
	mappers::ProvinceMapper provinceMapper;
	mappers::ReligionMapper religionMapper;
	mappers::SuccessionLawMapper successionLawMapper;
	mappers::TagTitleMapper tagTitleMapper;
	mappers::TraitMapper traitMapper;
	std::shared_ptr<mappers::CK3RegionMapper> ck3RegionMapper;
	std::shared_ptr<mappers::ImperatorRegionMapper> imperatorRegionMapper;
	TitlesHistory titlesHistory;


	std::set<std::string> countyHoldersCache;  // used by removeInvalidLandlessTitles
};

}  // namespace CK3



#endif	// CK3_WORLD_H