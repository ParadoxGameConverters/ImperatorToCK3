#include "AccessoryGenes.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"


Imperator::AccessoryGenes::AccessoryGenes(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::AccessoryGenes::registerKeys()
{
	registerKeyword("index", [this](std::istream& theStream) {
		index = commonItems::getInt(theStream);
	});
	registerRegex(R"([a-zA-Z0-9_]+)", [this](const std::string& geneName, std::istream& theStream) {
		genes.emplace(geneName, AccessoryGene(theStream));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
