#ifndef GOVERNMENT_MAPPER_H
#define GOVERNMENT_MAPPER_H

#include "Parser.h"
#include <map>
#include <optional>
#include <string>

namespace mappers
{
class GovernmentMapper: commonItems::parser
{
  public:
	GovernmentMapper();
	explicit GovernmentMapper(std::istream& theStream);

	[[nodiscard]] std::optional<std::string> getCK3GovernmentForImperatorGovernment(const std::string& impGovernment) const;

  private:
	void registerKeys();

	std::map<std::string, std::string> impToCK3GovernmentMap;
};
} // namespace mappers

#endif // GOVERNMENT_MAPPER_H
