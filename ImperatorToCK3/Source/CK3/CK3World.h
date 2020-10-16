#ifndef CK3_WORLD
#define CK3_WORLD


#include "../Imperator/ImperatorWorld.h"
#include "../Mappers/VersionParser/VersionParser.h"
#include "../Mappers/LocalizationMapper/LocalizationMapper.h"
#include "../Mappers/TagTitleMapper/TagTitleMapper.h"
#include "../Mappers/CultureMapper/CultureMapper.h"
#include "../Mappers/ReligionMapper/ReligionMapper.h"
#include "../Mappers/ProvinceMapper/ProvinceMapper.h"
#include "../Mappers/CoaMapper/CoaMapper.h"
#include "../Mappers/TraitMapper/TraitMapper.h"
#include "Character/CK3Character.h"
#include "Titles/LandedTitles.h"
#include "Province/CK3Province.h"
#include "Titles/TitlesHistory.h"

class Configuration;

namespace CK3
{

class World
{
	public:
		World(const Imperator::World& impWorld, const Configuration& theConfiguration, const mappers::VersionParser& versionParser);

		[[nodiscard]] const auto& getCharacters() const { return characters; }
		[[nodiscard]] const auto& getTitles() const { return titles; }
		[[nodiscard]] const auto& getProvinces() const { return provinces; }

	private:
		void importImperatorCharacters(const Imperator::World& impWorld, bool ConvertBirthAndDeathDates, date endDate);
		void importImperatorCharacter(const std::pair<unsigned long long, std::shared_ptr<Imperator::Character>>& character, bool ConvertBirthAndDeathDates, date endDate);
		void importVanillaNonCountyNonBaronyTitles(const Imperator::World& impWorld);
		void importImperatorCountries(const Imperator::World& impWorld);
		void importImperatorCountry(const std::pair<unsigned long long, std::shared_ptr<Imperator::Country>>& country);
		void importVanillaProvinces(const std::string& ck3Path);
		void importImperatorProvinces(const Imperator::World& impWorld);
		void linkCountiesToTitleHolders(const Imperator::World& impWorld);
		void linkSpouses(const Imperator::World& impWorld);
		void linkMothersAndFathers(const Imperator::World& impWorld);
		void removeInvalidLandlessTitles();

		[[nodiscard]] std::optional<std::pair<unsigned long long, std::shared_ptr<Imperator::Province>>> determineProvinceSource(const std::vector<unsigned long long>& impProvinceNumbers,
			const Imperator::World& impWorld) const;


		std::map<std::string, std::shared_ptr<Character>> characters;
		std::map<std::string, std::shared_ptr<Title>> titles;
		std::map<unsigned long long, std::shared_ptr<Province>> provinces;

		mappers::LocalizationMapper localizationMapper;
		mappers::TagTitleMapper tagTitleMapper;
		mappers::ProvinceMapper provinceMapper;
		mappers::CultureMapper cultureMapper;
		mappers::ReligionMapper religionMapper;
		mappers::CoaMapper coaMapper;
		mappers::TraitMapper traitMapper;
		TitlesHistory titlesHistory;

		LandedTitles landedTitles;

		std::set<std::string> countyHoldersCache; // used by removeInvalidLandlessTitles
};

}



#endif // CK3_WORLD