#ifndef FAMILY_FACTORY_H
#define FAMILY_FACTORY_H



#include "Family.h"
#include "ConvenientParser.h"
#include <memory>



namespace Imperator {

class Family::Factory: commonItems::convenientParser {
  public:
	explicit Factory();
	std::unique_ptr<Family> getFamily(std::istream& theStream, unsigned long long theFamilyID);

  private:
	std::unique_ptr<Family> family;
};

} // namespace Imperator



#endif // FAMILY_FACTORY_H