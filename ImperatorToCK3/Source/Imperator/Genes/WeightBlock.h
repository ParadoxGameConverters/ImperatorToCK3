#ifndef IMPERATOR_WEIGHT_BLOC_H
#define IMPERATOR_WEIGHT_BLOC_H
#include "Parser.h"


namespace ImperatorWorld
{	
	class WeightBlock : commonItems::parser
	{
	public:
		WeightBlock() = default;
		explicit WeightBlock(std::istream& theStream);

		[[nodiscard]] unsigned int getAbsoluteWeight(const std::string& objectName);
		[[nodiscard]] std::optional<std::string> getMatchingObject(double percentAsDecimal); // argument must be in range <0; 1>

		[[nodiscard]] const auto& getSumOfAbsoluteWeights() const { return sumOfAbsoluteWeights; }

		void addObject(const std::string& objectName, int absoluteWeight);

	private:
		void registerKeys();

		unsigned int sumOfAbsoluteWeights = 0;
		std::vector<std::pair<std::string, unsigned int>> objectsVector;
	};
} // namespace ImperatorWorld

#endif // IMPERATOR_WEIGHT_BLOC_H
