#ifndef CK3_WORLD
#define CK3_WORLD


#include "../Imperator/ImperatorWorld.h"
#include "../Mappers/VersionParser/VersionParser.h"

class Configuration;

namespace CK3World
{

class World
{
	public:
		World(const ImperatorWorld::World& impWorld, const Configuration& theConfiguration, const mappers::VersionParser& versionParser);
		World(const ImperatorWorld::World& impWorld): outputModName(impWorld.getSaveName()) {};

		[[nodiscard]] const auto& getOutputModName() const { return outputModName; }

	private:
		std::string outputModName;
};

}



#endif // CK3_WORLD