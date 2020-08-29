#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/Imperator/Genes/Gene.h"
#include <sstream>


TEST(ImperatorWorld_GeneTests, indexCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tindex=69";
	input << "}";

	const ImperatorWorld::Gene theGene(input, "accessory_gene");

	ASSERT_EQ(69, theGene.getIndex());
}

TEST(ImperatorWorld_FamilyTests, indexDefaultsTo0)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Gene theGene(input);

	ASSERT_EQ(0, theGene.getIndex());
}
