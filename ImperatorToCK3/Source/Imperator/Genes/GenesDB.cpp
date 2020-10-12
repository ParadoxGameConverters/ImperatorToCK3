#include "GenesDB.h"
#include "Log.h"
#include "ParserHelpers.h"


Imperator::GenesDB::GenesDB(const std::string& thePath)
{
	registerKeys();
	parseFile(thePath);
	clearRegisteredKeywords();
}

Imperator::GenesDB::GenesDB(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::GenesDB::registerKeys()
{
	registerKeyword("accessory_genes", [this](const std::string& unused, std::istream& theStream) {
		accessoryGenes = AccessoryGenes(theStream);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
