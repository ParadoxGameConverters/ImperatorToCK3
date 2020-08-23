#include "Countries.h"
#include "Country.h"
#include "../Families/Families.h"
#include "Log.h"
#include "ParserHelpers.h"

ImperatorWorld::Countries::Countries(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::Countries::registerKeys()
{
	registerRegex("\\d+", [this](const std::string& countryID, std::istream& theStream) {
		auto newCountry = std::make_shared<Country>(theStream, std::stoi(countryID));
		countries.insert(std::pair(newCountry->getID(), newCountry));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


ImperatorWorld::CountriesBloc::CountriesBloc(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::CountriesBloc::registerKeys()
{
	registerKeyword("country_database", [this](const std::string& unused, std::istream& theStream) {
		countries = Countries(theStream);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


void ImperatorWorld::Countries::linkFamilies(const Families& theFamilies)
{
	auto counter = 0;
	const auto& families = theFamilies.getFamilies();
	for (const auto& country : countries)
	{
		if (!country.second->getFamilies().empty())
		{
			std::map<int, std::shared_ptr<Family>> newFamilies;
			for (const auto& family : country.second->getFamilies())
			{
				const auto& familyItr = families.find(family.first);
				if (familyItr != families.end())
				{
					newFamilies.insert(std::pair(familyItr->first, familyItr->second));
					counter++;
				}
				else
				{
					Log(LogLevel::Warning) << "Family ID: " << family.first << " has no definition!";
				}
			}
			country.second->setFamilies(newFamilies);
		}
	}
	Log(LogLevel::Info) << "<> " << counter << " families linked to countries.";
}