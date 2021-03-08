#ifndef SUCCESSION_LAW_MAPPER_H
#define SUCCESSION_LAW_MAPPER_H



#include "Parser.h"
#include <map>
#include <set>
#include <string>



namespace mappers {

class SuccessionLawMapper: commonItems::parser {
public:
	SuccessionLawMapper();
	explicit SuccessionLawMapper(std::istream& theStream);

	[[nodiscard]] std::set<std::string> getCK3LawsForImperatorLaws(const std::set<std::string>& laws) const;

private:
	void registerKeys();

	std::map<std::string, std::set<std::string>> impToCK3SuccessionLawMap;
};

} // namespace mappers



#endif // SUCCESSION_LAW_MAPPER_H
