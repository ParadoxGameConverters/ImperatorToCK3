#include "../../ImperatorToCK3/Source/Mappers/ReligionMapper/ReligionMapping.h"
#include "gtest/gtest.h"
#include <sstream>


TEST(Mappers_ReligionMappingTests, ck3ReligionDefaultsToBlank)
{
	std::stringstream input;
	input << "= {}";

	const mappers::ReligionMapping theMapping(input);

	ASSERT_TRUE(theMapping.getCK3Religion().empty());
}


TEST(Mappers_ReligionMappingTests, ck3ReligionCanBeSet)
{
	std::stringstream input;
	input << "= { ck3 = ck3Religion }";

	const mappers::ReligionMapping theMapping(input);

	ASSERT_EQ(theMapping.getCK3Religion(), "ck3Religion");
}


TEST(Mappers_ReligionMappingTests, imperatorReligionsDefaultToEmpty)
{
	std::stringstream input;
	input << "= {}";

	const mappers::ReligionMapping theMapping(input);

	ASSERT_TRUE(theMapping.getImperatorReligions().empty());
}


TEST(Mappers_ReligionMappingTests, imperatorReligionsCanBeSet)
{
	std::stringstream input;
	input << "= { imp = religion1 imp = religion2 }";

	const mappers::ReligionMapping theMapping(input);

	ASSERT_EQ(theMapping.getImperatorReligions().size(), 2);
	ASSERT_EQ(*theMapping.getImperatorReligions().find("religion1"), "religion1");
	ASSERT_EQ(*theMapping.getImperatorReligions().find("religion2"), "religion2");
}