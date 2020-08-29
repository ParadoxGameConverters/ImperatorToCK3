#include "WeightBloc.h"
#include "Log.h"
#include "ParserHelpers.h"


ImperatorWorld::WeightBloc::WeightBloc(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}

void ImperatorWorld::WeightBloc::registerKeys()
{
	registerRegex("\\d+", [this](const std::string& absoluteWeightStr, std::istream& theStream) {
		const auto newObjectName = commonItems::singleString(theStream).getString();
		addObject(newObjectName, stoi(absoluteWeightStr));
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}

void ImperatorWorld::WeightBloc::addObject(const std::string& objectName, int absoluteWeight)
{
	objectsMap.emplace(objectName, absoluteWeight);
	sumOfAbsoluteWeights += absoluteWeight;
}

std::optional<std::string> ImperatorWorld::WeightBloc::getMatchingObject(double percentAsDecimal)
{
	if (percentAsDecimal < 0 || percentAsDecimal > 1) throw std::runtime_error("percentAsDecimal should be in range <0;1>");
	
	unsigned int sumOfPrecedingAbsoluteWeights = 0;
	for (auto const& [key, val] : objectsMap)
	{
		sumOfPrecedingAbsoluteWeights += val;
		if (percentAsDecimal <= static_cast<double>(sumOfPrecedingAbsoluteWeights) / sumOfAbsoluteWeights) return key;
	}
	return std::nullopt; // only happens when objectsMap is empty
}
