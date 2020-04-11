#ifndef CK3_WORLD
#define CK3_WORLD



#include "../Imperator/ImperatorWorld.h"



namespace Ck3World
{

class World
{
	public:
		World(const ImperatorWorld::World& impWorld): modName(impWorld.getSaveName()) {};

		std::string getModName() const { return modName; }

	private:
		std::string modName;
};

}



#endif // CK3_WORLD