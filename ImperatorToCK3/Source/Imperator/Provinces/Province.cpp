#include "Province.h"
#include "Pop.h"
#include "Log.h"
#include "ParserHelpers.h"

ImperatorWorld::Province::Province(std::istream& theStream, int provID): provinceID(provID)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::Province::registerKeys()
{
	/*registerKeyword("name", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString nameStr(theStream);
		name = nameStr.getString();
	});*/
	registerKeyword("culture", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString cultureStr(theStream);
		culture = cultureStr.getString();
	});
	registerKeyword("religion", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString religionStr(theStream);
		religion = religionStr.getString();
	});
	registerKeyword("owner", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt ownerInt(theStream);
		owner = ownerInt.getInt();
	});
	registerKeyword("controller", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt controllerInt(theStream);
		controller = controllerInt.getInt();
	});
	registerRegex("pop", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt popInt(theStream);
		pops.insert(std::pair(popInt.getInt(), nullptr));
	});
	registerRegex("[A-Za-z0-9\\:_.-]+", commonItems::ignoreItem);
}
