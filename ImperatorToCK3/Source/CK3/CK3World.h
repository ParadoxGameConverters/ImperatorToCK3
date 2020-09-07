#ifndef CK3_WORLD
#define CK3_WORLD


#include "../Imperator/ImperatorWorld.h"
#include "../Mappers/VersionParser/VersionParser.h"
#include "../Mappers/CultureMapper/CultureMapper.h"
#include "../Mappers/ReligionMapper/ReligionMapper.h"
#include "../Mappers/ProvinceMapper/ProvinceMapper.h"

#include "Province/CK3Province.h"

class Configuration;

namespace CK3
{

class World
{
	public:
		World(const ImperatorWorld::World& impWorld, const Configuration& theConfiguration, const mappers::VersionParser& versionParser);

		[[nodiscard]] const auto& getOutputModName() const { return outputModName; }
		[[nodiscard]] const auto& getProvinces() const { return provinces; }

	private:
		void importVanillaProvinces(const std::string& ck3Path);
		void importImperatorProvinces(const ImperatorWorld::World& sourceWorld);

		[[nodiscard]] std::optional<std::pair<int, std::shared_ptr<ImperatorWorld::Province>>> determineProvinceSource(const std::vector<int>& impProvinceNumbers,
			const ImperatorWorld::World& sourceWorld) const;


		std::map<int, std::shared_ptr<Province>> provinces;

		mappers::ProvinceMapper provinceMapper;
		mappers::CultureMapper cultureMapper;
		mappers::ReligionMapper religionMapper;

		std::string outputModName;
};

}



#endif // CK3_WORLD