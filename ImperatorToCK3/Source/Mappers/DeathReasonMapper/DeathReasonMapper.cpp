#include "DeathReasonMapper.h"
#include "DeathReasonMapping.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



mappers::DeathReasonMapper::DeathReasonMapper() {
	Log(LogLevel::Info) << "-> Parsing death reason mappings.";
	registerKeys();
	parseFile("configurables/deathMappings.txt");
	clearRegisteredKeywords();
	Log(LogLevel::Info) << "<> Loaded " << impToCK3ReasonMap.size() << " death reason links.";
}


mappers::DeathReasonMapper::DeathReasonMapper(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void mappers::DeathReasonMapper::registerKeys() {
	registerKeyword("link", [this](std::istream& theStream) {
		const DeathReasonMapping mapping(theStream);
		for (const auto& impReason: mapping.impReasons) {
			if (mapping.ck3Reason)
				impToCK3ReasonMap.emplace(impReason, *mapping.ck3Reason);
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


std::optional<std::string> mappers::DeathReasonMapper::getCK3ReasonForImperatorReason(const std::string& impReason) const {
	const auto& mapping = impToCK3ReasonMap.find(impReason);
	if (mapping != impToCK3ReasonMap.end())
		return mapping->second;
	return std::nullopt;
}