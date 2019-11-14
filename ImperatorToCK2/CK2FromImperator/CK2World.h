#ifndef CK2_WORLD
#define CK2_WORLD



#include "../CK2Interface/CK2WorldInterface.h"
#include "../Imperator/ImperatorWorld.h"



namespace CK2World
{

class World: public CK2Interface::World
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