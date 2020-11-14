#ifndef NICKNAME_MAPPING_H
#define NICKNAME_MAPPING_H

#include "Parser.h"
#include <set>

namespace mappers
{
class NicknameMapping: commonItems::parser
{
public:
	explicit NicknameMapping(std::istream& theStream);

	std::set<std::string> impNicknames;
	std::optional<std::string> ck3Nickname;
private:
	void registerKeys();
};
} // namespace mappers

#endif // NICKNAME_MAPPING_H