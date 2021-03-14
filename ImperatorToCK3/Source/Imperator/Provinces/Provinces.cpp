#include "Provinces.h"
#include "Province.h"
#include "Pops.h"
#include "../Countries/Countries.h"
#include "../Countries/Country.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include <ranges>



Imperator::Provinces::Provinces(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void Imperator::Provinces::registerKeys() {
	registerRegex(commonItems::integerRegex, [this](const std::string& provID, std::istream& theStream) {
		std::shared_ptr<Province> newProvince = provinceFactory.getProvince(theStream, commonItems::stringToInteger<unsigned long long>(provID));
		provinces.emplace(newProvince->getID(), newProvince);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


void Imperator::Provinces::linkPops(const Pops& thePops) {
	auto counter = 0;
	const auto& pops = thePops.getPops();
	for (const auto& province : provinces | std::views::values) {
		if (!province->getPops().empty()) {
			std::map<unsigned long long, std::shared_ptr<Pop>> newPops;
			for (const auto& popID : province->getPops() | std::views::keys) {
				const auto& popItr = pops.find(popID);
				if (popItr != pops.end()) {
					newPops.emplace(popItr->first, popItr->second);
					++counter;
				}
				else {
					Log(LogLevel::Warning) << "Pop ID: " << popID << " has no definition!";
				}
			}
			province->setPops(newPops);
		}
	}
	Log(LogLevel::Info) << "<> " << counter << " pops linked to provinces.";
}


void Imperator::Provinces::linkCountries(const Countries& theCountries) {
	auto counter = 0;
	const auto& countries = theCountries.getCountries();
	for (const auto& province : provinces | std::views::values) {
		if (!province->getPops().empty()) {
			const auto& countryItr = countries.find(province->getOwner().first);
			if (countryItr != countries.end()) {
				// link both ways
				province->linkOwnerCountry(countryItr->second);
				countryItr->second->registerProvince(province);
				++counter;
			}
			else {
				Log(LogLevel::Warning) << "Country ID: " << province->getOwner().first << " has no definition!";
			}
		}
	}
	Log(LogLevel::Info) << "<> " << counter << " countries linked to provinces.";
}