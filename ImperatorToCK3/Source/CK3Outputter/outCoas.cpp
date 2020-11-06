#include "outCoas.h"
#include <filesystem>
#include <fstream>
#include "../CK3/Titles/Title.h"


void CK3::outputCoas(const std::string& outputModName, const std::map<std::string, std::shared_ptr<Title>>& titles)
{
	std::ofstream output("output/" + outputModName + "/common/coat_of_arms/coat_of_arms/fromImperator.txt"); // dumping all into one file
	if (!output.is_open())
		throw std::runtime_error(
			"Could not create coat of arms file: output/" + outputModName + "/common/coat_of_arms/coat_of_arms/fromImperator.txt");
	
	for (const auto& [titleName, title] : titles)
	{
		auto coa = title->coa;
		if (coa)
			output << titleName << *coa << "\n";
	}

	output.close();
}