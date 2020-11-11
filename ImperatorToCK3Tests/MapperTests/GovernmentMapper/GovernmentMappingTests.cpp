#include "../../ImperatorToCK3/Source/Mappers/GovernmentMapper/GovernmentMapping.h"
#include "gtest/gtest.h"
#include <sstream>


TEST(Mappers_GovernmentMappingTests, ck3GovernmentDefaultsToEmpty)
{
	std::stringstream input;
	input << "= {}";

	const mappers::GovernmentMapping theMapping(input);

	ASSERT_TRUE(theMapping.ck3Government.empty());
}


TEST(Mappers_GovernmentMappingTests, ck3GovernmentCanBeSet)
{
	std::stringstream input;
	input << "= { ck3 = ck3Government }";

	const mappers::GovernmentMapping theMapping(input);

	ASSERT_EQ("ck3Government", theMapping.ck3Government);
}


TEST(Mappers_GovernmentMappingTests, imperatorGovernmentsDefaultToEmpty)
{
	std::stringstream input;
	input << "= {}";

	const mappers::GovernmentMapping theMapping(input);

	ASSERT_TRUE(theMapping.impGovernments.empty());
}


TEST(Mappers_GovernmentMappingTests, imperatorGovernmentsCanBeSet)
{
	std::stringstream input;
	input << "= { imp = gov1 imp = gov2 }";

	const mappers::GovernmentMapping theMapping(input);

	ASSERT_EQ(2, theMapping.impGovernments.size());
	ASSERT_EQ("gov1", *theMapping.impGovernments.find("gov1"));
	ASSERT_EQ("gov2", *theMapping.impGovernments.find("gov2"));
}