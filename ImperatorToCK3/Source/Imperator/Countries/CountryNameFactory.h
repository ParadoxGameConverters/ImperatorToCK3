#ifndef COUNTRY_NAME_FACTORY_H
#define COUNTRY_NAME_FACTORY_H



#include "CountryName.h"
#include "Parser.h"
#include <memory>



namespace Imperator {

class CountryName::Factory: commonItems::parser
{
  public:
	explicit Factory();
	std::unique_ptr<CountryName> getCountryName(std::istream& theStream);

  private:
	std::unique_ptr<CountryName> countryName;
};

} // namespace Imperator



#endif // COUNTRY_NAME_FACTORY_H