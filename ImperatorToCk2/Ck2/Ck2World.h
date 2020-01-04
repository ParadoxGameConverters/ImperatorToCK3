#ifndef CK2_WORLD
#define CK2_WORLD



#include "../Imperator/ImperatorWorld.h"



namespace Ck2World
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



#endif // CK2_WORLD