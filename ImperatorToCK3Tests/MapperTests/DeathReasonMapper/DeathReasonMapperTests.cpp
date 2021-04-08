#include "gtest/gtest.h"
#include "Mappers/DeathReasonMapper/DeathReasonMapper.h"
#include <sstream>



TEST(Mappers_DeathReasonMapperTests, nonMatchGivesEmptyOptional)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Reasob imp = impReason }";

	const mappers::DeathReasonMapper theMapper(input);

	const auto& ck3Reason = theMapper.getCK3ReasonForImperatorReason("nonMatchingReason");
	ASSERT_FALSE(ck3Reason);
}


TEST(Mappers_DeathReasonMapperTests, ck3ReasonCanBeFound)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Reason imp = impReason }";

	const mappers::DeathReasonMapper theMapper(input);

	const auto& ck3Reason = theMapper.getCK3ReasonForImperatorReason("impReason");
	ASSERT_EQ("ck3Reason", ck3Reason);
}


TEST(Mappers_DeathReasonMapperTests, multipleImpReasonsCanBeInARule)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Reason imp = impReason imp = impReason2 }";

	const mappers::DeathReasonMapper theMapper(input);

	const auto& ck3Reason = theMapper.getCK3ReasonForImperatorReason("impReason2");
	ASSERT_EQ("ck3Reason", ck3Reason);
}


TEST(Mappers_DeathReasonMapperTests, correctRuleMatches)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Reason imp = impReason }";
	input << "link = { ck3 = ck3Reason2 imp = impReason2 }";

	const mappers::DeathReasonMapper theMapper(input);

	const auto& ck3Reason = theMapper.getCK3ReasonForImperatorReason("impReason2");
	ASSERT_EQ("ck3Reason2", ck3Reason);
}