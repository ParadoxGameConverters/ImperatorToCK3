#include "CharacterName.h"
#include "Log.h"
#include "ParserHelpers.h"

ImperatorWorld::CharacterName::CharacterName(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::CharacterName::registerKeys()
{
	registerKeyword("name", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString nameStr(theStream);
		name = nameStr.getString();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}