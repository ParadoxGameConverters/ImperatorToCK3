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

	ASSERT_EQ("ck3Religion", theMapping.getCK3Religion());
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

	ASSERT_EQ(2, theMapping.getImperatorReligions().size());
	ASSERT_EQ("religion1", *theMapping.getImperatorReligions().find("religion1"));
	ASSERT_EQ("religion2", *theMapping.getImperatorReligions().find("religion2"));
}