#include "ProvinceMappingsVersion.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

mappers::ProvinceMappingsVersion::ProvinceMappingsVersion(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::ProvinceMappingsVersion::registerKeys()
{
	registerKeyword("link", [this](std::istream& theStream) {
		const ProvinceMapping newMapping(theStream);
		if (newMapping.getCK3Provinces().empty() && newMapping.getImpProvinces().empty())
			return;
		mappings.push_back(newMapping);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
