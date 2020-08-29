#ifndef IMPERATOR_WEIGHT_BLOC_H
#define IMPERATOR_WEIGHT_BLOC_H
#include "Parser.h"


namespace ImperatorWorld
{	
	class WeightBloc : commonItems::parser
	{
	public:
		WeightBloc() = default;
		WeightBloc(std::istream& theStream);

		[[nodiscard]] const auto& getAbsoluteWeight(const std::string& objectName) const { return objectsMap.find(objectName)->second; }
		[[nodiscard]] const auto& getSumOfAbsoluteWeights() const { return sumOfAbsoluteWeights; }
		[[nodiscard]] std::optional<std::string> getMatchingObject(double percentAsDecimal); // argument must be in range <0; 1>

		void addObject(const std::string& objectName, int absoluteWeight); // add the object to the map, add object's absolute weight to sumOfAbsoluteWeights

	private:
		void registerKeys();

		unsigned int sumOfAbsoluteWeights = 0;
		std::map<std::string, unsigned int> objectsMap; // do not add objects directly to this map, use AddObject
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_WEIGHT_BLOC_H
