#ifndef RELIGION_MAPPER_H
#define RELIGION_MAPPER_H

#include "Parser.h"
#include <map>
#include <optional>
#include <string>

namespace mappers
{
class ReligionMapper: commonItems::parser
{
  public:
	ReligionMapper();
	explicit ReligionMapper(std::istream& theStream);

	[[nodiscard]] std::optional<std::string> getCK3ReligionForImperatorReligion(const std::string& impReligion) const;

  private:
	void registerKeys();

	std::map<std::string, std::string> impToCK3ReligionMap;
};
} // namespace mappers

#endif // RELIGION_MAPPER_H
