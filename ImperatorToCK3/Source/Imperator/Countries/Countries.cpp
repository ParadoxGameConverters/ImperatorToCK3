#include "Countries.h"
#include "Country.h"
#include "../Families/Families.h"
#include "Log.h"
#include "ParserHelpers.h"

Imperator::Countries::Countries(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::Countries::registerKeys()
{
	registerRegex("\\d+", [this](const std::string& countryID, std::istream& theStream) {
		auto newCountry = std::make_shared<Country>(theStream, std::stoi(countryID));
		countries.insert(std::pair(newCountry->getID(), newCountry));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


Imperator::CountriesBloc::CountriesBloc(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::CountriesBloc::registerKeys()
{
	registerKeyword("country_database", [this](const std::string& unused, std::istream& theStream) {
		countries = Countries(theStream);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


void Imperator::Countries::linkFamilies(const Families& theFamilies)
{
	auto counter = 0;
	const auto& families = theFamilies.getFamilies();
	for (const auto& [countryID, country] : countries)
	{
		if (!country->getFamilies().empty())
		{
			std::map<int, std::shared_ptr<Family>> newFamilies;
			for (const auto& [familyID, family] : country->getFamilies())
			{
				const auto& familyItr = families.find(familyID);
				if (familyItr != families.end())
				{
					newFamilies.insert(std::pair(familyItr->first, familyItr->second));
					counter++;
				}
				else
				{
					Log(LogLevel::Warning) << "Family ID: " << familyID << " has no definition!";
				}
			}
			country->setFamilies(newFamilies);
		}
	}
	Log(LogLevel::Info) << "<> " << counter << " families linked to countries.";
}