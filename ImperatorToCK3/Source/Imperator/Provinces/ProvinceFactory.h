#ifndef PROVINCE_FACTORY_H
#define PROVINCE_FACTORY_H



#include "Province.h"
#include "ConvenientParser.h"



namespace Imperator {

class Province::Factory: commonItems::convenientParser {
  public:
	explicit Factory();
	std::unique_ptr<Province> getProvince(std::istream& theStream, unsigned long long provID);

  private:
	std::unique_ptr<Province> province;
};

} // namespace Imperator



#endif // PROVINCE_FACTORY_H