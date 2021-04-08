#include "gtest/gtest.h"
#include "Imperator/Genes/AccessoryGenes.h"
#include <sstream>



TEST(ImperatorWorld_AccessoryGenesTests, indexCanBeSet) {
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tindex=69";
	input << "}";

	const Imperator::AccessoryGenes theGenes(input);

	ASSERT_EQ(69, theGenes.getIndex());
}


TEST(ImperatorWorld_AccessoryGenesTests, indexDefaultsTo0) {
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const Imperator::AccessoryGenes theGenes(input);

	ASSERT_EQ(0, theGenes.getIndex());
}

TEST(ImperatorWorld_AccessoryGenesTests, genesDefaultToEmpty) {
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const Imperator::AccessoryGenes theGenes(input);

	ASSERT_TRUE(theGenes.getGenes().empty());
}

TEST(ImperatorWorld_AccessoryGenesTests, accessoryGenesAreProperlyLoaded) {
	std::stringstream input;
	input << "= {\n";
	input << "index= 5\n";
	input << "\thairstyles = {\n";
	input << "\t\tindex = 1\n";
	input << "\t}\n";
	input << "\tclothes = {\n";
	input << "\t\tindex = 2\n";
	input << "\t}\n";
	input << "}";


	const Imperator::AccessoryGenes theGenes(input);
	ASSERT_EQ(5, theGenes.getIndex());
	ASSERT_EQ(2, theGenes.getGenes().size());
	ASSERT_EQ(1, theGenes.getGenes().find("hairstyles")->second.getIndex());
	ASSERT_EQ(2, theGenes.getGenes().find("clothes")->second.getIndex());
}