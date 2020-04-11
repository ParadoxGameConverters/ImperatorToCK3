#include "Ck3/Ck3World.h"
#include "Ck3Outputter/Ck3WorldOutputter.h"
#include "Imperator/ImperatorWorld.h"



int main()
{
	ImperatorWorld::World impWorld;
	Ck3World::World ck3World(impWorld);
	Ck3World::outputWorld(ck3World);

	return 0;
}