#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/Imperator/Genes/AccessoryGene.h"
#include <sstream>


TEST(ImperatorWorld_AccessoryGeneTests, indexCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tindex=69";
	input << "}";

	const Imperator::AccessoryGene theGene(input);

	ASSERT_EQ(69, theGene.getIndex());
}

TEST(ImperatorWorld_AccessoryGeneTests, indexDefaultsTo0)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const Imperator::AccessoryGene theGene(input);

	ASSERT_EQ(0, theGene.getIndex());
}

TEST(ImperatorWorld_AccessoryGeneTests, geneTemplatesDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const Imperator::AccessoryGene theGene(input);

	ASSERT_TRUE(theGene.getGeneTemplates().empty());
}

TEST(ImperatorWorld_AccessoryGeneTests, accessoryGeneIsProperlyLoaded)
{
	std::stringstream input;
	input << " = {\n";
	input << "	index = 95\n";
	input << "	inheritable = no\n";
	input << "	nerdy_hairstyles = {\n";
	input << "			index = 0\n";
	input << "			male = {\n";
	input << "				6 = male_hair_roman_5\n";
	input << "				1 = empty\n";
	input << "			}\n";
	input << "			female = {\n";
	input << "				1 = female_hair_roman_1\n";
	input << "				1 = female_hair_roman_5\n";
	input << "			}\n";
	input << "			boy = male\n";
	input << "			girl = {\n";
	input << "				1 = female_hair_roman_1\n";
	input << "				1 = female_hair_roman_5\n";
	input << "			}\n";
	input << "	}\n";
	input << "	punk_hairstyles = {\n";
	input << "		index = 1\n";
	input << "		male = {\n";
	input << "				6 = male_hair_roman_1\n";
	input << "				1 = empty\n";
	input << "		}\n";
	input << "		female = {\n";
	input << "				1 = female_hair_roman_1\n";
	input << "				1 = female_hair_roman_2\n";
	input << "		}\n";
	input << "		girl = female\n";
	input << "	} \n";
	input << "}\n";

	const Imperator::AccessoryGene gene(input);

	ASSERT_EQ(95, gene.getIndex());
	ASSERT_FALSE(gene.isInheritable());
	ASSERT_EQ(2, gene.getGeneTemplates().size());
	ASSERT_EQ(1, gene.getGeneTemplates().find("punk_hairstyles")->second.getIndex());
	ASSERT_EQ(4, gene.getGeneTemplates().find("nerdy_hairstyles")->second.getAgeSexWeightBlocs().size());
	ASSERT_EQ(3, gene.getGeneTemplates().find("punk_hairstyles")->second.getAgeSexWeightBlocs().size());
}