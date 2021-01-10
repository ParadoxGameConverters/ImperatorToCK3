#include "VersionParser.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"

mappers::VersionParser::VersionParser()
{
	registerKeys();
	parseFile("configurables/version.txt");
	clearRegisteredKeywords();
}

mappers::VersionParser::VersionParser(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void mappers::VersionParser::registerKeys()
{
	registerKeyword("name", [this](std::istream& theStream) {
		name = commonItems::getString(theStream);
	});
	registerKeyword("version", [this](std::istream& theStream) {
		version = commonItems::getString(theStream);
	});
	registerKeyword("descriptionLine", [this](std::istream& theStream) {
		descriptionLine = commonItems::getString(theStream);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
