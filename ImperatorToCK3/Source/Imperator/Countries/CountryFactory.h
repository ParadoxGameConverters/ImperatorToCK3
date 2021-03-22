#ifndef COUNTRY_FACTORY_H
#define COUNTRY_FACTORY_H



#include "Country.h"
#include "CountryNameFactory.h"
#include "Parser.h"
#include <memory>



namespace Imperator {

class Country::Factory: commonItems::parser
{
public:
	explicit Factory();
	std::unique_ptr<Country> getCountry(std::istream& theStream, unsigned long long countryID);

private:
	std::unique_ptr<Country> country;
	CountryName::Factory countryNameFactory;
};

} // namespace Imperator



#endif // COUNTRY_FACTORY_H