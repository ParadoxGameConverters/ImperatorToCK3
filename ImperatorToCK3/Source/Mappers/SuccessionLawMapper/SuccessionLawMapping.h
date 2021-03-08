#ifndef SUCCESSION_LAW_MAPPING_H
#define SUCCESSION_LAW_MAPPING_H



#include "Parser.h"
#include <set>



namespace mappers {

class SuccessionLawMapping: commonItems::parser {
public:
	explicit SuccessionLawMapping(std::istream& theStream);

	std::string impLaw;
	std::set<std::string> ck3SuccessionLaws;
};

} // namespace mappers



#endif // SUCCESSION_LAW_MAPPING_H