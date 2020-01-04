#ifndef CK2_WORLD
#define CK2_WORLD



#include "../Ck2Interface/Ck2WorldInterface.h"
#include "../Imperator/ImperatorWorld.h"



namespace Ck2World
{

class World: public Ck2Interface::World
{
	public:
		World(const ImperatorWorld::World& impWorld): message(impWorld.getMessage()) {};
		virtual ~World() {};
		virtual std::string getMessage() const { return message; }

	private:
		std::string message;
};

}



#endif // CK2_WORLD