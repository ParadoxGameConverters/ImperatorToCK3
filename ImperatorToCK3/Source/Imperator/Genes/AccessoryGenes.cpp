#include "AccessoryGenes.h"
#include "Log.h"
#include "ParserHelpers.h"


ImperatorWorld::AccessoryGenes::AccessoryGenes(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::AccessoryGenes::registerKeys()
{
	registerKeyword("index", [this](const std::string& unused, std::istream& theStream) {
		index = commonItems::singleInt(theStream).getInt();
		});
	registerRegex(R"([a-zA-Z0-9_]+)", [this](const std::string& geneName, std::istream& theStream) {
		genes.emplace(geneName, AccessoryGene(theStream));
		});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
