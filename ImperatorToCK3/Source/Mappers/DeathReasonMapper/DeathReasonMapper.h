#ifndef DEATH_REASON_MAPPER_H
#define DEATH_REASON_MAPPER_H



#include "Parser.h"
#include <map>
#include <optional>
#include <string>



namespace mappers {

class DeathReasonMapper: commonItems::parser {
public:
	DeathReasonMapper();
	explicit DeathReasonMapper(std::istream& theStream);

	[[nodiscard]] std::optional<std::string> getCK3ReasonForImperatorReason(const std::string& impReason) const;

private:
	void registerKeys();

	std::map<std::string, std::string> impToCK3ReasonMap;
};

} // namespace mappers



#endif // DEATH_REASON_MAPPER_H
