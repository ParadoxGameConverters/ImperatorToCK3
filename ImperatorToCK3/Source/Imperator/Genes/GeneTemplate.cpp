#include "GeneTemplate.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "WeightBloc.h"


ImperatorWorld::GeneTemplate::GeneTemplate(std::istream& theStream, const std::string& geneTypeStr)
{
	geneType = geneTypeStr;
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::GeneTemplate::registerKeys()
{
	registerKeyword("index", [this](const std::string& unused, std::istream& theStream) {
		const commonItems::singleInt indexInt(theStream);
		index = indexInt.getInt();
		Log(LogLevel::Debug) << index;
	});
	registerRegex("male|female|boy|girl", [this](const std::string& ageSexStr, std::istream& theStream) {
		if (geneType == "accessory_gene")
		{
			auto ageSexBloc = std::make_shared<WeightBloc>(theStream);
			ageSexWeightBlocs.insert(std::pair(ageSexStr, ageSexBloc));
			Log(LogLevel::Debug) << "inserted weight bloc for agesex " << ageSexStr;
		}
		/*else
		{
			auto ageSexBloc = std::make_shared<SettingsBloc>(theStream);
			ageSexBlocs.insert(std::pair(ageSexStr, ageSexBloc));
			Log(LogLevel::Debug) << "inserted bloc for agesex " << ageSexStr;
		}*/
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
