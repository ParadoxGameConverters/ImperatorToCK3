#include "Gene.h"
#include "Log.h"
#include "ParserHelpers.h"


ImperatorWorld::Gene::Gene(std::istream& theStream, const std::string& geneTypeStr)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::Gene::registerKeys()
{
	registerKeyword("index", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt indexInt(theStream);
		index = indexInt.getInt();
		Log(LogLevel::Debug) << index;
	});
	registerRegex(R"([a-zA-Z0-9_]+)", [this](const std::string& geneTemplateName, std::istream& theStream) {
		geneTemplates.insert({ geneTemplateName, GeneTemplate(theStream, geneType) });
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
