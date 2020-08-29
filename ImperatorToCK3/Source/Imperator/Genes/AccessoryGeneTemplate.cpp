#include "AccessoryGeneTemplate.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "WeightBloc.h"


ImperatorWorld::AccessoryGeneTemplate::AccessoryGeneTemplate(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::AccessoryGeneTemplate::registerKeys()
{
	registerKeyword("index", [this](const std::string& unused, std::istream& theStream) {
		index = commonItems::singleInt{ theStream }.getInt();
	});
	registerRegex("male|female|boy|girl", [this](const std::string& ageSexStr, std::istream& theStream) {
		const auto sexAge = commonItems::singleItem(ageSexStr, theStream);
		if (sexAge.find('{') != std::string::npos)
		{
			std::stringstream tempStream(sexAge);
			auto ageSexBloc = std::make_shared<WeightBloc>(tempStream);
			ageSexWeightBlocs.insert(std::pair(ageSexStr, ageSexBloc));
		}
		else // for copies: "boy = male"
		{
			ageSexWeightBlocs.insert(std::pair(ageSexStr, ageSexWeightBlocs.find(sexAge)->second));
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
