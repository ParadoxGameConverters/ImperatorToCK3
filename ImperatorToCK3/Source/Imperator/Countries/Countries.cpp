#include "Countries.h"
#include "Country.h"
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