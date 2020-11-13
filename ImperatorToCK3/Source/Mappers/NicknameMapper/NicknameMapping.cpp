#include "NicknameMapping.h"
#include "ParserHelpers.h"

mappers::NicknameMapping::NicknameMapping(std::istream& theStream)
{
	registerKeyword("ck3", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString nicknameString(theStream);
		ck3Nickname = nicknameString.getString();
	});
	registerKeyword("imp", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString nicknameString(theStream);
		impNicknames.insert(nicknameString.getString());
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}