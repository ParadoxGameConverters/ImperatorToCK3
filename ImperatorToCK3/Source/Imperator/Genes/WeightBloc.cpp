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
		try
		{
			addObject(newObjectName, stoi(absoluteWeightStr));
		}
		catch (const std::invalid_argument& ia)
		{
			Log(LogLevel::Error) << "Could not add object to WeightBlock: Invalid argument: " << ia.what();
		}
		catch (const std::out_of_range& oor)
		{
			Log(LogLevel::Info) << "Could not add object to WeightBlock: Out of Range error: " << oor.what();
		}
		catch (const std::exception& e)
		{
			Log(LogLevel::Info) << "Could not add object to WeightBlock: Undefined error: " << e.what();
		}
	});
	registerRegex(commonItems::catchallRegex, commonItems::ignoreItem);
}


void ImperatorWorld::WeightBloc::addObject(const std::string& objectName, int absoluteWeight)
{
	objectsVector.emplace_back(objectName, absoluteWeight);
	sumOfAbsoluteWeights += absoluteWeight;
}


unsigned int ImperatorWorld::WeightBloc::getAbsoluteWeight(const std::string& objectName)
{
	for (auto const& [key, val] : objectsVector)
	{
		if (key == objectName) return val;
	}
	return 0;
}


std::optional<std::string> ImperatorWorld::WeightBloc::getMatchingObject(double percentAsDecimal)
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
