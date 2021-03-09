#include "CK3Provinces.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



CK3::Provinces::Provinces(const std::string& filePath) {
	registerKeys();
	parseFile(filePath);
	clearRegisteredKeywords();
}


void CK3::Provinces::registerKeys() {
	registerRegex(commonItems::integerRegex, [this](const std::string& provIdStr, std::istream& theStream) {
		auto provID = commonItems::stringToInteger<unsigned long long>(provIdStr);
		auto newProvince = std::make_shared<Province>(provID, theStream);
		provinces.emplace(provID, newProvince);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}