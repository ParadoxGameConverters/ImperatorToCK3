#ifndef IMPERATOR_COUNTRY_NAME_H
#define IMPERATOR_COUNTRY_NAME_H
#include "Parser.h"

namespace Imperator
{
class CountryName : commonItems::parser
{
  public:
	  CountryName() = default;
	explicit CountryName(std::istream& theStream);

	[[nodiscard]] const auto& getName() const { return name; }

  private:
	void registerKeys();

	std::string name;
};
} // namespace Imperator

#endif // IMPERATOR_COUNTRY_NAME_H