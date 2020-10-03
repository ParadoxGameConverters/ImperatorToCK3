#include "../../ImperatorToCK3/Source/Mappers/TraitMapper/TraitMapping.h"
#include "gtest/gtest.h"
#include <sstream>


TEST(Mappers_TraitMappingTests, ck3TraitDefaultsToNullopt)
{
	std::stringstream input;
	input << "= {}";

	const mappers::TraitMapping theMapping(input);

	ASSERT_FALSE(theMapping.ck3Trait);
}


TEST(Mappers_TraitMappingTests, ck3TraitCanBeSet)
{
	std::stringstream input;
	input << "= { ck3 = ck3Trait }";

	const mappers::TraitMapping theMapping(input);

	ASSERT_EQ("ck3Trait", theMapping.ck3Trait);
}


TEST(Mappers_TraitMappingTests, imperatorTraitsDefaultToEmpty)
{
	std::stringstream input;
	input << "= {}";

	const mappers::TraitMapping theMapping(input);

	ASSERT_TRUE(theMapping.impTraits.empty());
}


TEST(Mappers_TraitMappingTests, imperatorTraitsCanBeSet)
{
	std::stringstream input;
	input << "= { imp = trait1 imp = trait2 }";

	const mappers::TraitMapping theMapping(input);

	ASSERT_EQ(2, theMapping.impTraits.size());
	ASSERT_EQ("trait1", *theMapping.impTraits.find("trait1"));
	ASSERT_EQ("trait2", *theMapping.impTraits.find("trait2"));
}