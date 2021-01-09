#include "Provinces.h"
#include "Province.h"
#include "Pops.h"
#include "../Countries/Countries.h"
#include "../Countries/Country.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

Imperator::Provinces::Provinces(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::Provinces::registerKeys()
{
	registerRegex(R"(\d+)", [this](const std::string& provID, std::istream& theStream) {
		auto newProvince = std::make_shared<Province>(theStream, std::stoull(provID));
		provinces.insert(std::pair(newProvince->getID(), newProvince));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

void Imperator::Provinces::linkPops(const Pops& thePops)
{
	auto counter = 0;
	const auto& pops = thePops.getPops();
	for (const auto& [provinceID, province] : provinces)
	{
		if (!province->getPops().empty())
		{
			std::map<unsigned long long, std::shared_ptr<Pop>> newPops;
			for (const auto& [popID, pop] : province->getPops())
			{
				const auto& popItr = pops.find(popID);
				if (popItr != pops.end())
				{
					newPops.insert(std::pair(popItr->first, popItr->second));
					counter++;
				}
				else
				{
					Log(LogLevel::Warning) << "Pop ID: " << popID << " has no definition!";
				}
			}
			province->setPops(newPops);
		}
	}
	Log(LogLevel::Info) << "<> " << counter << " pops linked to provinces.";
}

void Imperator::Provinces::linkCountries(const Countries& theCountries)
{
	auto counter = 0;
	const auto& countries = theCountries.getCountries();
	for (const auto& [provinceID, province] : provinces)
	{
		if (!province->getPops().empty())
		{
			const auto& countryItr = countries.find(province->getOwner());
			if (countryItr != countries.end())
			{
				// link both ways
				province->country = countryItr->second;
				countryItr->second->registerProvince(province);
				++counter;
			}
			else
			{
				Log(LogLevel::Warning) << "Country ID: " << province->getOwner() << " has no definition!";
			}
		}
	}
	Log(LogLevel::Info) << "<> " << counter << " countries linked to provinces.";
}