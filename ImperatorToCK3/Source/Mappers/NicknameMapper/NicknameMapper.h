#ifndef NICKNAME_MAPPER_H
#define NICKNAME_MAPPER_H

#include "Parser.h"
#include <map>
#include <optional>
#include <string>

namespace mappers
{
class NicknameMapper: commonItems::parser
{
  public:
	NicknameMapper();
	explicit NicknameMapper(std::istream& theStream);

	[[nodiscard]] std::optional<std::string> getCK3NicknameForImperatorNickname(const std::string& impNickname) const;

  private:
	void registerKeys();

	std::map<std::string, std::string> impToCK3NicknameMap;
};
} // namespace mappers

#endif // NICKNAME_MAPPER_H
