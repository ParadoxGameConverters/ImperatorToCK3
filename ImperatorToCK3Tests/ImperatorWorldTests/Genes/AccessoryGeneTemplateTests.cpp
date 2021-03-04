#include "gtest/gtest.h"
#include <sstream>
#include "Imperator/Genes/AccessoryGeneTemplate.h"
#include "Imperator/Genes/WeightBlock.h"


TEST(ImperatorWorld_AccessoryGeneTemplateTests, ageSexWeightBlocksDefaultsToEmpty) {
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const Imperator::AccessoryGeneTemplate geneTemplate(input);

	ASSERT_TRUE(geneTemplate.getAgeSexWeightBlocs().empty());
}

TEST(ImperatorWorld_AccessoryGeneTemplateTests, ageSexWeightBlocksCanBeLoaded) {
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "male={}\n";
	input << "female={}\n";
	input << "boy=male\n";
	input << "girl=female\n";
	input << "}";

	const Imperator::AccessoryGeneTemplate geneTemplate(input);

	ASSERT_EQ(4, geneTemplate.getAgeSexWeightBlocs().size());
}

TEST(ImperatorWorld_AccessoryGeneTemplateTests, ageSexWithBlocksAreProperlyCopied) {
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "male={ 6 = hoodie 8 = trousers }\n";
	input << "female={ 4 = skirt 6 = top }\n";
	input << "boy=male\n";
	input << "girl=female\n";
	input << "}";

	const Imperator::AccessoryGeneTemplate geneTemplate(input);

	ASSERT_EQ(6, geneTemplate.getAgeSexWeightBlocs().find("male")->second->getAbsoluteWeight("hoodie"));
	ASSERT_EQ(8, geneTemplate.getAgeSexWeightBlocs().find("male")->second->getAbsoluteWeight("trousers"));
	ASSERT_EQ(4, geneTemplate.getAgeSexWeightBlocs().find("female")->second->getAbsoluteWeight("skirt"));
	ASSERT_EQ(6, geneTemplate.getAgeSexWeightBlocs().find("female")->second->getAbsoluteWeight("top"));
	ASSERT_EQ(6, geneTemplate.getAgeSexWeightBlocs().find("boy")->second->getAbsoluteWeight("hoodie"));
	ASSERT_EQ(8, geneTemplate.getAgeSexWeightBlocs().find("boy")->second->getAbsoluteWeight("trousers"));
	ASSERT_EQ(4, geneTemplate.getAgeSexWeightBlocs().find("girl")->second->getAbsoluteWeight("skirt"));
	ASSERT_EQ(6, geneTemplate.getAgeSexWeightBlocs().find("girl")->second->getAbsoluteWeight("top"));
}
