#include "GovernmentMapper.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "GovernmentMapping.h"

mappers::GovernmentMapper::GovernmentMapper()
{
	LOG(LogLevel::Info) << "-> Parsing government mappings.";
	registerKeys();
	parseFile("configurables/government_map.txt");
	clearRegisteredKeywords();
	LOG(LogLevel::Info) << "<> Loaded " << impToCK3GovernmentMap.size() << " government links.";
}

mappers::GovernmentMapper::GovernmentMapper(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::GovernmentMapper::registerKeys()
{
	registerKeyword("link", [this](const std::string& unused, std::istream& theStream) {
		const GovernmentMapping theMapping(theStream);
		for (const auto& imperatorGovernment: theMapping.impGovernments)
		{
			if (theMapping.ck3Government) impToCK3GovernmentMap.emplace(imperatorGovernment, *theMapping.ck3Government);
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

std::optional<std::string> mappers::GovernmentMapper::getCK3GovernmentForImperatorGovernment(const std::string& impGovernment) const
{
	const auto& mapping = impToCK3GovernmentMap.find(impGovernment);
	if (mapping != impToCK3GovernmentMap.end())
		return mapping->second;
	return std::nullopt;
}