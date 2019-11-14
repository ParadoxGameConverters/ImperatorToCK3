#include "Ck2WorldOutputter.h"
#include <fstream>



void Ck2WorldOutputter::outputWorld(const Ck2Interface::World& world)
{
	std::ofstream output("output.txt");
	output << world.getMessage();
	output.close();
}