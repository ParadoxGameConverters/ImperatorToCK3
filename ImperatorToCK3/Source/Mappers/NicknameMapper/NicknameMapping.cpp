#include "NicknameMapping.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

mappers::NicknameMapping::NicknameMapping(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::NicknameMapping::registerKeys()
{
	registerKeyword("ck3", [this](std::istream& theStream) {
		const commonItems::singleString nicknameString(theStream);
		ck3Nickname = nicknameString.getString();
	});
	registerKeyword("imp", [this](std::istream& theStream) {
		const commonItems::singleString nicknameString(theStream);
		impNicknames.insert(nicknameString.getString());
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

