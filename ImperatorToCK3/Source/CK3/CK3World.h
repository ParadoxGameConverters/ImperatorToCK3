#ifndef CK3_WORLD_H
#define CK3_WORLD_H



#include "Mappers/LocalizationMapper/LocalizationMapper.h"
#include "Mappers/TagTitleMapper/TagTitleMapper.h"
#include "Mappers/CultureMapper/CultureMapper.h"
#include "Mappers/ReligionMapper/ReligionMapper.h"
#include "Mappers/ProvinceMapper/ProvinceMapper.h"
#include "Mappers/CoaMapper/CoaMapper.h"
#include "Mappers/TraitMapper/TraitMapper.h"
#include "Mappers/NicknameMapper/NicknameMapper.h"
#include "Mappers/GovernmentMapper/GovernmentMapper.h"
#include "Mappers/RegionMapper/CK3RegionMapper.h"
#include "Mappers/RegionMapper/ImperatorRegionMapper.h"
#include "Mappers/SuccessionLawMapper/SuccessionLawMapper.h"
#include "Character/CK3Character.h"
#include "Dynasties/Dynasty.h"
#include "Province/CK3Province.h"
#include "Titles/LandedTitles.h"
#include "Titles/Title.h"
#include "Titles/TitlesHistory.h"



class Configuration;

namespace Imperator {
class World;
}

namespace commonItems {
struct ConverterVersion;
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
	void importImperatorCharacter(const std::pair<unsigned long long, std::shared_ptr<Imperator::Character>>& character, bool ConvertBirthAndDeathDates, date endDate);
	void linkSpouses();
	void linkMothersAndFathers();

	void importImperatorFamilies(const Imperator::World& impWorld);
	
	void importImperatorCountries(const Imperator::World& impWorld);
	void importImperatorCountry(const std::pair<unsigned long long, std::shared_ptr<Imperator::Country>>& country);
	
	void importVanillaProvinces(const std::string& ck3Path);
	void importImperatorProvinces(const Imperator::World& impWorld);

	void addHistoryToVanillaTitles();
	void overWriteCountiesHistory(const Imperator::World& impWorld);
	void removeInvalidLandlessTitles();

	[[nodiscard]] std::optional<std::pair<unsigned long long, std::shared_ptr<Imperator::Province>>> determineProvinceSource(const std::vector<unsigned long long>& impProvinceNumbers,
		const Imperator::World& impWorld) const;


	std::map<std::string, std::shared_ptr<Character>> characters;
	std::map<std::string, std::shared_ptr<Dynasty>> dynasties;
	LandedTitles landedTitles;
	std::map<unsigned long long, std::shared_ptr<Province>> provinces;

	mappers::LocalizationMapper localizationMapper;
	mappers::TagTitleMapper tagTitleMapper;
	mappers::ProvinceMapper provinceMapper;
	mappers::CultureMapper cultureMapper;
	mappers::ReligionMapper religionMapper;
	mappers::CoaMapper coaMapper;
	mappers::TraitMapper traitMapper;
	mappers::NicknameMapper nicknameMapper;
	mappers::GovernmentMapper governmentMapper;
	std::shared_ptr<mappers::CK3RegionMapper> ck3RegionMapper;
	std::shared_ptr<mappers::ImperatorRegionMapper> imperatorRegionMapper;
	mappers::SuccessionLawMapper successionLawMapper;
	TitlesHistory titlesHistory;


	std::set<std::string> countyHoldersCache; // used by removeInvalidLandlessTitles
};

}



#endif // CK3_WORLD_H