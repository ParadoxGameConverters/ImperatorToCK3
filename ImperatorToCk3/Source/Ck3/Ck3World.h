#ifndef CK3_WORLD
#define CK3_WORLD



#include "../Imperator/ImperatorWorld.h"

class Configuration;

namespace Ck3World
{

class World
{
	public:
		World(const ImperatorWorld::World& impWorld, const Configuration& theConfiguration);
		World(const ImperatorWorld::World& impWorld): outputModName(impWorld.getSaveName()) {};

		std::string getOutputModName() const { return outputModName; }

	private:
		std::string outputModName;
};

}



#endif // CK3_WORLD