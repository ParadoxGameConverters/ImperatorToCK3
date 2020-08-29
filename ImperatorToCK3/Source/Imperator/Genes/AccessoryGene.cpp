#include "AccessoryGene.h"
#include "Log.h"
#include "ParserHelpers.h"


ImperatorWorld::AccessoryGene::AccessoryGene(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::AccessoryGene::registerKeys()
{
	registerKeyword("index", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt indexInt(theStream);
		index = indexInt.getInt();
	});
	registerKeyword("inheritable", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleString boolString(theStream);
		if (boolString.getString() == "yes") inheritable = true;
	});
	registerRegex(R"([a-zA-Z0-9_]+)", [this](const std::string& geneTemplateName, std::istream& theStream) {
		geneTemplates.insert({ geneTemplateName, AccessoryGeneTemplate(theStream) });
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
