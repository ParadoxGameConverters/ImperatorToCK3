#ifndef CK3_WORLD
#define CK3_WORLD


#include "../Imperator/ImperatorWorld.h"
#include "../Mappers/VersionParser/VersionParser.h"
#include "../Mappers/LocalizationMapper/LocalizationMapper.h"
#include "../Mappers/TagTitleMapper/TagTitleMapper.h"
#include "../Mappers/CultureMapper/CultureMapper.h"
#include "../Mappers/ReligionMapper/ReligionMapper.h"
#include "../Mappers/ProvinceMapper/ProvinceMapper.h"
#include "Titles/LandedTitles.h"

#include "Province/CK3Province.h"

class Configuration;

namespace CK3
{

class World
{
	public:
		World(const ImperatorWorld::World& impWorld, const Configuration& theConfiguration, const mappers::VersionParser& versionParser);

		[[nodiscard]] const auto& getOutputModName() const { return outputModName; }
		[[nodiscard]] const auto& getTitles() const { return titles; }
		[[nodiscard]] const auto& getProvinces() const { return provinces; }

	private:
		void importImperatorCountries(const ImperatorWorld::World& sourceWorld);
		void importImperatorCountry(const std::pair<int, std::shared_ptr<ImperatorWorld::Country>>& country, const ImperatorWorld::World& sourceWorld);
		void importVanillaProvinces(const std::string& ck3Path);
		void importImperatorProvinces(const ImperatorWorld::World& sourceWorld);

		[[nodiscard]] std::optional<std::pair<int, std::shared_ptr<ImperatorWorld::Province>>> determineProvinceSource(const std::vector<int>& impProvinceNumbers,
			const ImperatorWorld::World& sourceWorld) const;


		std::map<std::string, std::shared_ptr<Title>> titles;
		std::map<int, std::shared_ptr<Province>> provinces;

		mappers::LocalizationMapper localizationMapper;
		mappers::TagTitleMapper tagTitleMapper;
		mappers::ProvinceMapper provinceMapper;
		mappers::CultureMapper cultureMapper;
		mappers::ReligionMapper religionMapper;


		LandedTitles landedTitles;			// title name, title

		std::string outputModName;
};

}



#endif // CK3_WORLD