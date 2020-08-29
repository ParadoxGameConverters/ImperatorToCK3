#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/Imperator/Genes/WeightBloc.h"
#include <sstream>

#include "../../../ImperatorToCK3/Source/Imperator/Families/Family.h"


TEST(ImperatorWorld_WeightBlocTests, objectsCanBeAdded)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "	5 = female_hair_greek_1\n";
	input << "	2 = female_hair_greek_2\n";
	input << "	6 = female_hair_greek_3\n";
	input << "}";

	ImperatorWorld::WeightBloc weightBloc(input);

	ASSERT_EQ(5, weightBloc.getAbsoluteWeight("female_hair_greek_1"));
	ASSERT_EQ(2, weightBloc.getAbsoluteWeight("female_hair_greek_2"));
	ASSERT_EQ(6, weightBloc.getAbsoluteWeight("female_hair_greek_3"));
	ASSERT_EQ(13, weightBloc.getSumOfAbsoluteWeights());

	ASSERT_EQ("female_hair_greek_1", weightBloc.getMatchingObject(0.37234234).value());
	ASSERT_EQ("female_hair_greek_2", weightBloc.getMatchingObject(0.52234234234).value());
	ASSERT_EQ("female_hair_greek_3", weightBloc.getMatchingObject(1).value());
}
TEST(ImperatorWorld_WeightBlocTests, objectsCanBeAddedByMethod)
{
	ImperatorWorld::WeightBloc weightBloc;
	weightBloc.addObject("new_object", 69);
	weightBloc.addObject("new_object2", 5);
	ASSERT_EQ(69, weightBloc.getAbsoluteWeight("new_object"));
	ASSERT_EQ(5, weightBloc.getAbsoluteWeight("new_object2"));
	ASSERT_EQ(74, weightBloc.getSumOfAbsoluteWeights());

	ASSERT_EQ("new_object", weightBloc.getMatchingObject(0).value());
	ASSERT_EQ("new_object2", weightBloc.getMatchingObject(0.94).value());
}

TEST(ImperatorWorld_WeightBlocTests, sumOfAbsoluteWeightsDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	ImperatorWorld::WeightBloc weightBloc(input);

	ASSERT_EQ(0, weightBloc.getSumOfAbsoluteWeights());
}

TEST(ImperatorWorld_WeightBlocTests, getMatchingObjectThrowsErrorOnNegativeArgument)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "	2 = female_hair_greek_2\n";
	input << "}";

	ImperatorWorld::WeightBloc weightBloc(input);

	ASSERT_THROW(auto matchingObject = weightBloc.getMatchingObject(-0.5), std::runtime_error);
}

TEST(ImperatorWorld_WeightBlocTests, getMatchingObjectThrowsErrorOnArgumentGreaterThan1)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "	2 = female_hair_greek_2\n";
	input << "}";

	ImperatorWorld::WeightBloc weightBloc(input);

	ASSERT_THROW(auto matchingObject = weightBloc.getMatchingObject(1.234), std::runtime_error);
}

TEST(ImperatorWorld_WeightBlocTests, getMatchingObjectReturnsNulloptWhenObjectsMapIsEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	ImperatorWorld::WeightBloc weightBloc(input);

	ASSERT_FALSE(weightBloc.getMatchingObject(0.345));
}
