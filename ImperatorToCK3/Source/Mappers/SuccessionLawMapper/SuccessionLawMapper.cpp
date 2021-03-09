#include "SuccessionLawMapper.h"
#include "SuccessionLawMapping.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



mappers::SuccessionLawMapper::SuccessionLawMapper() {
	LOG(LogLevel::Info) << "-> Parsing succession law mappings.";
	registerKeys();
	parseFile("configurables/succession_law_map.txt");
	clearRegisteredKeywords();
	LOG(LogLevel::Info) << "<> Loaded " << impToCK3SuccessionLawMap.size() << " succession law links.";
}


mappers::SuccessionLawMapper::SuccessionLawMapper(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void mappers::SuccessionLawMapper::registerKeys() {
	registerKeyword("link", [this](std::istream& theStream) {
		const SuccessionLawMapping mapping(theStream);
		if (mapping.ck3SuccessionLaws.empty())
			throw std::runtime_error("SuccessionLawMapper: link with no CK3 successions laws");
		
		auto [iterator, inserted] = impToCK3SuccessionLawMap.emplace(mapping.impLaw, mapping.ck3SuccessionLaws);
		if (!inserted) {
			iterator->second.insert(mapping.ck3SuccessionLaws.begin(), mapping.ck3SuccessionLaws.end());
		}
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}


std::set<std::string> mappers::SuccessionLawMapper::getCK3LawsForImperatorLaws(const std::set<std::string>& laws) const {
	std::set<std::string> lawsToReturn;
	for (const auto& impLaw : laws) {
		const auto& mapItr = impToCK3SuccessionLawMap.find(impLaw);
		if (mapItr != impToCK3SuccessionLawMap.end()) {
			lawsToReturn.insert(mapItr->second.begin(), mapItr->second.end());
		}
	}
	return lawsToReturn;
}