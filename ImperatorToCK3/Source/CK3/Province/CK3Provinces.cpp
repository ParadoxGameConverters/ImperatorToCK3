#include "CK3Provinces.h"
#include "Log.h"
#include "ParserHelpers.h"

CK3::Provinces::Provinces(const std::string& filePath)
{
	registerKeys();
	parseFile(filePath);
	clearRegisteredKeywords();
}

void CK3::Provinces::registerKeys()
{
	registerRegex(R"(\d+)", [this](const std::string& provID, std::istream& theStream) {
		auto newProvince = std::make_shared<Province>(std::stoull(provID), theStream);
		provinces.insert(std::pair(std::stoull(provID), newProvince));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}