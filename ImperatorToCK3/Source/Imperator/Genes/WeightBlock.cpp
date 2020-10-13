#include "WeightBlock.h"
#include "Log.h"
#include "ParserHelpers.h"


Imperator::WeightBlock::WeightBlock(std::istream& theStream)
{
	registerKeys();
	parseStream(theStream);
	clearRegisteredKeywords();
}


void Imperator::WeightBlock::registerKeys()
{
	registerRegex("\\d+", [this](const std::string& absoluteWeightStr, std::istream& theStream) {
		const auto newObjectName = commonItems::singleString(theStream).getString();
		try
		{
			addObject(newObjectName, stoi(absoluteWeightStr));
		}
		catch (const std::exception& e)
		{
			Log(LogLevel::Error) << "Undefined error, absolute weight value was: " << absoluteWeightStr << "; Error message: " << e.what();
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


unsigned int Imperator::WeightBlock::getAbsoluteWeight(const std::string& objectName)
{
	for (auto const& [key, val] : objectsVector)
	{
		if (key == objectName) return val;
	}
	return 0;
}


std::optional<std::string> Imperator::WeightBlock::getMatchingObject(const double percentAsDecimal)
{
	if (percentAsDecimal < 0 || percentAsDecimal > 1) throw std::runtime_error("percentAsDecimal should be in range <0;1>");
	
	unsigned int sumOfPrecedingAbsoluteWeights = 0;
	for (auto const& [key, val] : objectsVector)
	{
		sumOfPrecedingAbsoluteWeights += val;
		if (sumOfAbsoluteWeights > 0 && percentAsDecimal <= static_cast<double>(sumOfPrecedingAbsoluteWeights)/sumOfAbsoluteWeights) return key;
	}
	return std::nullopt; // only happens when objectsMap is empty
}


void Imperator::WeightBlock::addObject(const std::string& objectName, int absoluteWeight)
{
	objectsVector.emplace_back(objectName, absoluteWeight);
	sumOfAbsoluteWeights += absoluteWeight;
}