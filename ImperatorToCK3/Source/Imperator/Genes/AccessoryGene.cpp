#include "AccessoryGene.h"
#include "Log.h"
#include "ParserHelpers.h"


Imperator::AccessoryGene::AccessoryGene(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::AccessoryGene::registerKeys()
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

const std::pair<const std::string, Imperator::AccessoryGeneTemplate>& Imperator::AccessoryGene::getGeneTemplateByIndex(const unsigned int indexInDna)
{
	for (auto& geneTemplateItr : geneTemplates)
	{
		if (geneTemplateItr.second.getIndex() == indexInDna) return geneTemplateItr;
	}
	LOG(LogLevel::Warning) << "Could not find gene template by index from DNA";
	return *geneTemplates.begin(); // fallback: return first element
}
