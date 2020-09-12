#include "outCoas.h"
#include <filesystem>
#include <fstream>
#include "../CK3/Titles/Title.h"


void CK3::outputCoas(const std::string& outputModName, const World& CK3World)
{
	std::ofstream output("output/" + outputModName + "/common/coat_of_arms/coat_of_arms/fromImperator.txt"); // dumping all into one file
	if (!output.is_open())
		throw std::runtime_error(
			"Could not create landed titles file: output/" + outputModName + "/common/coat_of_arms/coat_of_arms/fromImperator.txt");
	
	for (const auto& [first, second] : CK3World.getTitles())
	{
		//auto coa = title.second->getImperatorCountry().second->getFlag();
		auto coa = second->getCoa();
		if (coa)
			output << first << " = " << coa.value() << "\n";
	}

	output.close();
}