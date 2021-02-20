#ifndef COUNTRY_FACTORY_H
#define COUNTRY_FACTORY_H



#include "ConvenientParser.h"
#include "Country.h"
#include <memory>



namespace Imperator
{

class Country::Factory: commonItems::convenientParser
{
  public:
	explicit Factory();
	std::unique_ptr<Country> getCountry(std::istream& theStream, unsigned long long countryID);

  private:
	std::unique_ptr<Country> country;
};

} // namespace Imperator



#endif // COUNTRY_FACTORY_H