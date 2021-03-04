#include "GovernmentMapper.h"
#include "GovernmentMapping.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



mappers::GovernmentMapper::GovernmentMapper() {
	LOG(LogLevel::Info) << "-> Parsing government mappings.";
	registerKeys();
	parseFile("configurables/government_map.txt");
	clearRegisteredKeywords();
	LOG(LogLevel::Info) << "<> Loaded " << impToCK3GovernmentMap.size() << " government links.";
}


mappers::GovernmentMapper::GovernmentMapper(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void mappers::GovernmentMapper::registerKeys() {
	registerKeyword("link", [this](std::istream& theStream) {
		const GovernmentMapping theMapping(theStream);
		if (theMapping.ck3Government.empty())
			throw std::runtime_error("GovernmentMapper: link with no ck3Government");
		
		for (const auto& imperatorGovernment : theMapping.impGovernments) {
			impToCK3GovernmentMap.emplace(imperatorGovernment, theMapping.ck3Government);
		}
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}


std::optional<std::string> mappers::GovernmentMapper::getCK3GovernmentForImperatorGovernment(const std::string& impGovernment) const {
	const auto& mapping = impToCK3GovernmentMap.find(impGovernment);
	if (mapping != impToCK3GovernmentMap.end())
		return mapping->second;
	return std::nullopt;
}