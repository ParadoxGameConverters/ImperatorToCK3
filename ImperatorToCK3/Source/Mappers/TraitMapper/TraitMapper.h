#ifndef TRAIT_MAPPER_H
#define TRAIT_MAPPER_H



#include "Parser.h"
#include <map>
#include <optional>
#include <string>



namespace mappers {

class TraitMapper: commonItems::parser {
  public:
	TraitMapper();
	explicit TraitMapper(std::istream& theStream);

	[[nodiscard]] std::optional<std::string> getCK3TraitForImperatorTrait(const std::string& impTrait) const;

  private:
	void registerKeys();

	std::map<std::string, std::string> impToCK3TraitMap;
};

} // namespace mappers



#endif // TRAIT_MAPPER_H
