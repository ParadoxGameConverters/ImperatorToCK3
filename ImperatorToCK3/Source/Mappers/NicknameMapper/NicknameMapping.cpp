#include "NicknameMapping.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



mappers::NicknameMapping::NicknameMapping(std::istream& theStream) {
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void mappers::NicknameMapping::registerKeys() {
	registerKeyword("ck3", [this](std::istream& theStream) {
		ck3Nickname = commonItems::getString(theStream);
	});
	registerKeyword("imp", [this](std::istream& theStream) {
		impNicknames.insert(commonItems::getString(theStream));
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}
