#include "gtest/gtest.h"
#include <sstream>


#include "../ImperatorToCK3/Source/Imperator/Genes/GenesDB.h"
#include "../ImperatorToCK3/Source/Imperator/Genes/AccessoryGenes.h"


TEST(ImperatorWorld_GenesTests, genesDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::GenesDB genes(input);

	ASSERT_TRUE(genes.getAccessoryGenes().getGenes().empty());
}

TEST(ImperatorWorld_GenesTests, accessoryGenesCanBeLoadedInsideGeneGroup)
{
	std::stringstream input;
	input << "accessory_genes = {\n";
	input << "\thairstyles={ index = 1}\n";
	input << "\tclothes={ index =2}\n";
	input << "}";

	ImperatorWorld::GenesDB genes(input);
	const auto& geneItr = genes.getAccessoryGenes().getGenes().find("hairstyles");
	const auto& geneItr2 = genes.getAccessoryGenes().getGenes().find("clothes");


	ASSERT_EQ("hairstyles", geneItr->first);
	ASSERT_EQ("clothes", geneItr2->first);
	ASSERT_EQ(1, geneItr->second.getIndex());
	ASSERT_EQ(2, geneItr2->second.getIndex());
	ASSERT_EQ(2, genes.getAccessoryGenes().getGenes().size());
}

TEST(ImperatorWorld_GenesTests, simpleParameterCanBeLoadedInsideGeneGroup)
{
	std::stringstream input;
	input << "accessory_genes = {\n";
	input << "\tindex = 65\n";
	input << "}";

	const ImperatorWorld::GenesDB genes(input);

	ASSERT_EQ(65, genes.getAccessoryGenes().getIndex());
}