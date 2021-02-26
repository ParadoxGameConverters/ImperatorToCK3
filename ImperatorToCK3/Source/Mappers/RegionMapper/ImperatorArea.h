#ifndef IMPERATOR_AREA_H
#define IMPERATOR_AREA_H

#include "Parser.h"
#include <set>

namespace mappers
{
class ImperatorArea: commonItems::parser
{
  public:
	explicit ImperatorArea(std::istream& theStream);

	[[nodiscard]] const auto& getProvinces() const { return provinces; }
	[[nodiscard]] bool areaContainsProvince(unsigned long long province) const;

  private:
	void registerKeys();
	std::set<unsigned long long> provinces;
};
} // namespace mappers

#endif // IMPERATOR_AREA_H