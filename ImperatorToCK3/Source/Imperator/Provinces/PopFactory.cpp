#include "PopFactory.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



Imperator::Pop::Factory::Factory()
{
	registerKeyword("type", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString typeStr(theStream);
		pop->type = typeStr.getString();
	});
	registerKeyword("culture", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString cultureStr(theStream);
		pop->culture = cultureStr.getString();
	});
	registerKeyword("religion", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString religionStr(theStream);
		pop->religion = religionStr.getString();
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


std::unique_ptr<Imperator::Pop> Imperator::Pop::Factory::getPop(const std::string& idString, std::istream& theStream)
{
	pop = std::make_unique<Pop>();
	pop->ID = std::stoull(idString);

	parseStream(theStream);

	return std::move(pop);
}