#ifndef TRAIT_MAPPING_H
#define TRAIT_MAPPING_H

#include "Parser.h"
#include <set>

namespace mappers
{
class TraitMapping: commonItems::parser
{
  public:
	explicit TraitMapping(std::istream& theStream);

	std::set<std::string> impTraits;
	std::string ck3Trait;
};
} // namespace mappers

#endif // TRAIT_MAPPING_H