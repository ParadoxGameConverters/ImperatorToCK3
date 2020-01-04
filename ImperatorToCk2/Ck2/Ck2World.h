#ifndef CK2_WORLD
#define CK2_WORLD



#include "../Imperator/ImperatorWorld.h"



namespace Ck2World
{

class World
{
	public:
		World(const ImperatorWorld::World& impWorld): message(impWorld.getMessage()) {};

		std::string getMessage() const { return message; }

	private:
		std::string message;
};

}



#endif // CK2_WORLD