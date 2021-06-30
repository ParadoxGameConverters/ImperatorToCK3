#include "NicknameMapper.h"
#include "NicknameMapping.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



mappers::NicknameMapper::NicknameMapper() {
	Log(LogLevel::Info) << "-> Parsing nickname mappings.";
	registerKeys();
	parseFile("configurables/nickname_map.txt");
	clearRegisteredKeywords();
	Log(LogLevel::Info) << "<> Loaded " << impToCK3NicknameMap.size() << " nickname links.";
}


mappers::NicknameMapper::NicknameMapper(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void mappers::NicknameMapper::registerKeys() {
	registerKeyword("link", [this](std::istream& theStream) {
		const NicknameMapping theMapping(theStream);
		for (const auto& imperatorNickname: theMapping.impNicknames) {
			if (theMapping.ck3Nickname)
				impToCK3NicknameMap.emplace(imperatorNickname, *theMapping.ck3Nickname);
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


std::optional<std::string> mappers::NicknameMapper::getCK3NicknameForImperatorNickname(const std::string& impNickname) const
{
	const auto& mapping = impToCK3NicknameMap.find(impNickname);
	if (mapping != impToCK3NicknameMap.end())
		return mapping->second;
	return std::nullopt;
}
