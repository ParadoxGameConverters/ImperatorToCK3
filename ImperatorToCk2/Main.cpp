#include "Ck2FromImperator/Ck2World.h"
#include "Ck2Outputter/Ck2WorldOutputter.h"
#include "Imperator/ImperatorWorld.h"



int main()
{
	ImperatorWorld::World impWorld;
	Ck2World::World ck2World(impWorld);
	Ck2WorldOutputter::outputWorld(ck2World);

	return 0;
}