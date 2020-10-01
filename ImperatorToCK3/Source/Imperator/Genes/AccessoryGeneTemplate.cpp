#include "AccessoryGeneTemplate.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "WeightBlock.h"


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
		const auto sexAge = commonItems::stringOfItem(theStream).getString();
		if (sexAge.find('{') != std::string::npos) // for full blocks: "male = { 6 = hoodie 7 = tshirt }"
		{
			std::stringstream tempStream(sexAge);
			auto ageSexBlock = std::make_shared<WeightBlock>(tempStream);
			ageSexWeightBlocks.insert(std::pair(ageSexStr, ageSexBlock));
		}
		else if (ageSexWeightBlocks.find(sexAge) != ageSexWeightBlocks.end()) // for copies: "boy = male"
		{
			ageSexWeightBlocks.insert(std::pair(ageSexStr, ageSexWeightBlocks.find(sexAge)->second));
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
