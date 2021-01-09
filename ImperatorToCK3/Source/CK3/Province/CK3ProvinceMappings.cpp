#include "CK3ProvinceMappings.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

CK3::ProvinceMappings::ProvinceMappings(const std::string& theFile)
{
	registerKeys();
	parseFile(theFile);
	clearRegisteredKeywords();
}

void CK3::ProvinceMappings::registerKeys()
{
	registerRegex(R"(\d+)", [this](const std::string& provID, std::istream& theStream) {
		auto baseProvID = commonItems::singleULlong{ theStream }.getULlong();
		if (stoull(provID) != baseProvID) mappings.insert(std::pair(std::stoull(provID), baseProvID)); // if left and right IDs are equal, no point in mapping
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}