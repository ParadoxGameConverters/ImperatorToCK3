#include "gtest/gtest.h"
#include "Mappers/DeathReasonMapper/DeathReasonMapping.h"
#include <sstream>



TEST(Mappers_DeathReasonMappingTests, ck3ReasonDefaultsToNullopt) {
	std::stringstream input;

	const mappers::DeathReasonMapping theMapping(input);

	ASSERT_FALSE(theMapping.ck3Reason);
}


TEST(Mappers_DeathReasonMappingTests, ck3ReasonCanBeSet) {
	std::stringstream input;
	input << "= { ck3 = ck3Trait }";

	const mappers::DeathReasonMapping theMapping(input);

	ASSERT_EQ("ck3Trait", theMapping.ck3Reason);
}


TEST(Mappers_DeathReasonMappingTests, imperatorReasonsDefaultToEmpty) {
	std::stringstream input;

	const mappers::DeathReasonMapping theMapping(input);

	ASSERT_TRUE(theMapping.impReasons.empty());
}


TEST(Mappers_DeathReasonMappingTests, imperatorReasonsCanBeSet) {
	std::stringstream input;
	input << "= { imp = reason_dumb imp = reason_bear }";

	const mappers::DeathReasonMapping theMapping(input);

	ASSERT_EQ(2, theMapping.impReasons.size());
	ASSERT_TRUE(theMapping.impReasons.contains("reason_dumb"));
	ASSERT_TRUE(theMapping.impReasons.contains("reason_bear"));
}