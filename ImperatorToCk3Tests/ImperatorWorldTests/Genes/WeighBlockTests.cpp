#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/Imperator/Genes/WeightBlock.h"
#include <sstream>


TEST(ImperatorWorld_WeightBlocTests, objectsCanBeAdded)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\t5 = female_hair_greek_1\n";
	input << "\t2 = sdfsdf\n";
	input << "\t6 = random\n";
	input << "}";

	ImperatorWorld::WeightBlock weightBlock(input);

	ASSERT_EQ(5, weightBlock.getAbsoluteWeight("female_hair_greek_1"));
	ASSERT_EQ(2, weightBlock.getAbsoluteWeight("sdfsdf"));
	ASSERT_EQ(6, weightBlock.getAbsoluteWeight("random"));
	ASSERT_EQ(13, weightBlock.getSumOfAbsoluteWeights());

	ASSERT_EQ("female_hair_greek_1", weightBlock.getMatchingObject(0.37234234).value());
	ASSERT_EQ("sdfsdf", weightBlock.getMatchingObject(0.52234234234).value());
	ASSERT_EQ("random", weightBlock.getMatchingObject(1).value());
}
TEST(ImperatorWorld_WeightBlocTests, objectsCanBeAddedByMethod)
{
	ImperatorWorld::WeightBlock weightBlock;
	weightBlock.addObject("new_object", 69);
	weightBlock.addObject("new_object2", 5);
	ASSERT_EQ(69, weightBlock.getAbsoluteWeight("new_object"));
	ASSERT_EQ(5, weightBlock.getAbsoluteWeight("new_object2"));
	ASSERT_EQ(74, weightBlock.getSumOfAbsoluteWeights());

	ASSERT_EQ("new_object", weightBlock.getMatchingObject(0).value());
	ASSERT_EQ("new_object2", weightBlock.getMatchingObject(0.95).value());
}

TEST(ImperatorWorld_WeightBlocTests, sumOfAbsoluteWeightsDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::WeightBlock weightBlock(input);

	ASSERT_EQ(0, weightBlock.getSumOfAbsoluteWeights());
}

TEST(ImperatorWorld_WeightBlocTests, getMatchingObjectThrowsErrorOnNegativeArgument)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\t2 = female_hair_greek_2\n";
	input << "}";

	ImperatorWorld::WeightBlock weightBlock(input);

	ASSERT_THROW(auto matchingObject = weightBlock.getMatchingObject(-0.5), std::runtime_error);
}

TEST(ImperatorWorld_WeightBlocTests, getMatchingObjectThrowsErrorOnArgumentGreaterThan1)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\t2 = female_hair_greek_2\n";
	input << "}";

	ImperatorWorld::WeightBlock weightBlock(input);

	ASSERT_THROW(auto matchingObject = weightBlock.getMatchingObject(1.234), std::runtime_error);
}

TEST(ImperatorWorld_WeightBlocTests, getMatchingObjectReturnsNulloptWhenObjectsMapIsEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	ImperatorWorld::WeightBlock weightBlock(input);

	ASSERT_FALSE(weightBlock.getMatchingObject(0.345));
}
