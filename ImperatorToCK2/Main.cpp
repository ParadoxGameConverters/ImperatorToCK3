#include "CK2FromImperator/CK2World.h"
#include "CK2Outputter/CK2WorldOutputter.h"
#include "Imperator/ImperatorWorld.h"



int main()
{
	ImperatorWorld::World impWorld;
	CK2World::World ck2World(impWorld);
	CK2WorldOutputter::outputWorld(ck2World);

	return 0;
}