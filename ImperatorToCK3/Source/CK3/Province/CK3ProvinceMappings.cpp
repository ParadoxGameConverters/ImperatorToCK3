#include "CK3ProvinceMappings.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



CK3::ProvinceMappings::ProvinceMappings(const std::string& theFile) {
	registerKeys();
	parseFile(theFile);
	clearRegisteredKeywords();
}


void CK3::ProvinceMappings::registerKeys() {
	registerMatcher(commonItems::integerMatch, [this](const std::string& provIDStr, std::istream& theStream) {
		auto targetProvID = commonItems::stringToInteger<unsigned long long>(provIDStr);
		auto baseProvID = commonItems::getULlong(theStream);
		if (targetProvID != baseProvID) // if left and right IDs are equal, no point in mapping
			mappings.emplace(targetProvID, baseProvID);
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}