#ifndef GOVERNMENT_MAPPING_H
#define GOVERNMENT_MAPPING_H

#include "Parser.h"
#include <set>

namespace mappers
{
class GovernmentMapping: commonItems::parser
{
  public:
	explicit GovernmentMapping(std::istream& theStream);

	std::set<std::string> impGovernments;
	std::optional<std::string> ck3Government;
};
} // namespace mappers

#endif // GOVERNMENT_MAPPING_H