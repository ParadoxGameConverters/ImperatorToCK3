#include "AccessoryGeneTemplate.h"
#include "Log.h"
#include "ParserHelpers.h"
#include "CommonRegexes.h"
#include "WeightBlock.h"


Imperator::AccessoryGeneTemplate::AccessoryGeneTemplate(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void Imperator::AccessoryGeneTemplate::registerKeys()
{
	registerKeyword("index", [this](const std::string& unused, std::istream& theStream) {
		index = commonItems::singleInt{ theStream }.getInt();
	});
	registerRegex("male|female|boy|girl", [this](const std::string& ageSexStr, std::istream& theStream) {
		const auto sexAgeStr = commonItems::stringOfItem(theStream).getString();
		std::stringstream tempStream(sexAgeStr);
		if (sexAgeStr.find('{') != std::string::npos) // for full blocks: "male = { 6 = hoodie 7 = tshirt }"
		{
			auto ageSexBlock = std::make_shared<WeightBlock>(tempStream);
			ageSexWeightBlocks.insert(std::pair(ageSexStr, ageSexBlock));
		}
		else // for copies: "boy = male"
		{
			const auto sexAge = commonItems::singleString(tempStream).getString();
			if (ageSexWeightBlocks.contains(sexAge))
				ageSexWeightBlocks.insert(std::pair(ageSexStr, ageSexWeightBlocks.find(sexAge)->second));
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}
