#ifndef POP_FACTORY_H
#define POP_FACTORY_H



#include "Parser.h"
#include "Pop.h"
#include <memory>



namespace Imperator
{

class Pop::Factory: commonItems::parser
{
  public:
	explicit Factory();
	std::unique_ptr<Pop> getPop(const std::string& idString, std::istream& theStream);

  private:
	std::unique_ptr<Pop> pop;
};

} // namespace Imperator



#endif // POP_FACTORY_H