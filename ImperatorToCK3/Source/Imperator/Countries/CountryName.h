#ifndef IMPERATOR_COUNTRY_NAME_H
#define IMPERATOR_COUNTRY_NAME_H
#include "Parser.h"

namespace ImperatorWorld
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
} // namespace ImperatorWorld

#endif // IMPERATOR_COUNTRY_NAME_H