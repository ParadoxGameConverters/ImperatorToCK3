#include "ReligionMapping.h"
#include "ParserHelpers.h"

mappers::ReligionMapping::ReligionMapping(std::istream& theStream)
{
	registerKeyword("ck3", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString religionString(theStream);
		ck3Religion = religionString.getString();
	});
	registerKeyword("imp", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString religionString(theStream);
		impReligions.insert(religionString.getString());
	});
	registerRegex("[a-zA-Z0-9\\_.:]+", commonItems::ignoreItem);

	parseStream(theStream);
	clearRegisteredKeywords();
}