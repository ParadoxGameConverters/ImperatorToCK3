#ifndef RELIGION_MAPPING_H
#define RELIGION_MAPPING_H

#include "Parser.h"
#include <set>

namespace mappers
{
class ReligionMapping: commonItems::parser
{
  public:
	explicit ReligionMapping(std::istream& theStream);

	[[nodiscard]] const auto& getImperatorReligions() const { return impReligions; }
	[[nodiscard]] const auto& getCK3Religion() const { return ck3Religion; }
	[[nodiscard]] const auto& getCK3Regions() const { return ck3Regions; }

  private:
	std::set<std::string> impReligions;
	std::string ck3Religion;
	std::set<std::string> ck3Regions;
};
} // namespace mappers

#endif // RELIGION_MAPPING_H