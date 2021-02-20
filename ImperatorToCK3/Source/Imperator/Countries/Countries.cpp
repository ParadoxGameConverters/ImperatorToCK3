#include "Countries.h"
#include "Country.h"
#include "../Families/Families.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"


Imperator::Countries::Countries(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::Countries::registerKeys()
{
	registerMatcher(commonItems::integerMatch, [this](const std::string& countryID, std::istream& theStream) {
		std::shared_ptr<Country> newCountry = countryFactory.getCountry(theStream, commonItems::stringToInteger<unsigned long long>(countryID));
		countries.emplace(newCountry->getID(), newCountry);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}


Imperator::CountriesBloc::CountriesBloc(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::CountriesBloc::registerKeys()
{
	registerKeyword("country_database", [this](std::istream& theStream) {
		countries = Countries(theStream);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}


void Imperator::Countries::linkFamilies(const Families& theFamilies)
{
	auto counter = 0;
	std::set<unsigned long long> idsWithoutDefinition;
	const auto& families = theFamilies.getFamilies();
	for (const auto& [countryID, country] : countries)
	{
		if (!country->getFamilies().empty())
		{
			std::map<unsigned long long, std::shared_ptr<Family>> newFamilies;
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
					idsWithoutDefinition.insert(familyID);
				}
			}
			country->setFamilies(newFamilies);
		}
	}

	std::string warningString = "Families without definition:";
	if (!idsWithoutDefinition.empty())
	{
		for (auto id : idsWithoutDefinition)
		{
			warningString += " ";
			warningString += std::to_string(id);
			warningString += ",";
		}
		warningString = warningString.substr(0, warningString.size() - 1); //remove last comma
		Log(LogLevel::Warning) << warningString;
	}
	
	Log(LogLevel::Info) << "<> " << counter << " families linked to countries.";
}