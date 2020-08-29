#include "Genes.h"
#include "Log.h"
#include "ParserHelpers.h"


ImperatorWorld::Genes::Genes(const std::string& thePath)
{
	registerKeys();
	parseFile(thePath);
	clearRegisteredKeywords();
}

ImperatorWorld::Genes::Genes(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::Genes::registerKeys()
{
	/*registerKeyword("morph_genes", [this](const std::string& unused, std::istream& theStream) {
		Log(LogLevel::Debug) << "<> just a test 1";
		loadMorphGenes(theStream);
	});*/
	registerKeyword("accessory_genes", [this](const std::string& unused, std::istream& theStream) {
		Log(LogLevel::Debug) << "<> entering accessory genes";
		accessoryGenes = AccessoryGenes(theStream);
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
