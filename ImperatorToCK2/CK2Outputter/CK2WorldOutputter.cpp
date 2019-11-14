#include "CK2WorldOutputter.h"
#include <fstream>



void CK2WorldOutputter::outputWorld(const CK2Interface::World& world)
{
	std::ofstream output("output.txt");
	output << world.getMessage();
	output.close();
}