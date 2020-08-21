#include "Pop.h"
#include "Log.h"
#include "ParserHelpers.h"

ImperatorWorld::Pop::Pop(std::istream& theStream, int thePopID) : popID(thePopID)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::Pop::registerKeys()
{
	registerKeyword("type", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString typeStr(theStream);
		type = typeStr.getString();
	});
	registerKeyword("culture", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString cultureStr(theStream);
		culture = cultureStr.getString();
	});
	registerKeyword("religion", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString religionStr(theStream);
		religion = religionStr.getString();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}