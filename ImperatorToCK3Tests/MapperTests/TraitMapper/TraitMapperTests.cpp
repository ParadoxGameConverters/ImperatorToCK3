#include "../../ImperatorToCK3/Source/Mappers/TraitMapper/TraitMapper.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_TraitMapperTests, nonMatchGivesEmptyOptional)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Trait imp = impTrait }";

	const mappers::TraitMapper theMapper(input);

	const auto& ck3Trait = theMapper.getCK3TraitForImperatorTrait("nonMatchingTrait");
	ASSERT_FALSE(ck3Trait);
}


TEST(Mappers_TraitMapperTests, ck3TraitCanBeFound)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Trait imp = impTrait }";

	const mappers::TraitMapper theMapper(input);

	const auto& ck3Trait = theMapper.getCK3TraitForImperatorTrait("impTrait");
	ASSERT_EQ("ck3Trait", ck3Trait);
}


TEST(Mappers_TraitMapperTests, multipleCK3TraitsCanBeInARule)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Trait imp = impTrait imp = impTrait2 }";

	const mappers::TraitMapper theMapper(input);

	const auto& ck3Trait = theMapper.getCK3TraitForImperatorTrait("impTrait2");
	ASSERT_EQ("ck3Trait", ck3Trait);
}


TEST(Mappers_TraitMapperTests, correctRuleMatches)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Trait imp = impTrait }";
	input << "link = { ck3 = ck3Trait2 imp = impTrait2 }";

	const mappers::TraitMapper theMapper(input);

	const auto& ck3Trait = theMapper.getCK3TraitForImperatorTrait("impTrait2");
	ASSERT_EQ("ck3Trait2", ck3Trait);
}