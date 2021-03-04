#include "AccessoryGene.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"



Imperator::AccessoryGene::AccessoryGene(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void Imperator::AccessoryGene::registerKeys() {
	registerKeyword("index", [this](std::istream& theStream) {
		index = commonItems::getInt(theStream);
	});
	registerKeyword("inheritable", [this](std::istream& theStream) {
		if (commonItems::getString(theStream) == "yes")
			inheritable = true;
	});
	registerMatcher(commonItems::stringMatch, [this](const std::string& geneTemplateName, std::istream& theStream) {
		geneTemplates.insert({ geneTemplateName, AccessoryGeneTemplate(theStream) });
	});
	registerMatcher(commonItems::catchallRegexMatch, commonItems::ignoreItem);
}


const std::pair<const std::string, Imperator::AccessoryGeneTemplate>& Imperator::AccessoryGene::getGeneTemplateByIndex(const unsigned int indexInDna)
{
	for (auto& geneTemplateItr : geneTemplates) {
		if (geneTemplateItr.second.getIndex() == indexInDna)
			return geneTemplateItr;
	}
	LOG(LogLevel::Warning) << "Could not find gene template by index from DNA";
	return *geneTemplates.begin(); // fallback: return first element
}
