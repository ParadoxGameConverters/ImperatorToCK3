#ifndef DEATH_REASON_MAPPING_H
#define DEATH_REASON_MAPPING_H



#include "Parser.h"
#include <set>



namespace mappers {

class DeathReasonMapping: commonItems::parser {
public:
	explicit DeathReasonMapping(std::istream& theStream);

	std::set<std::string> impReasons;
	std::optional<std::string> ck3Reason;
};

} // namespace mappers



#endif // DEATH_REASON_MAPPING_H