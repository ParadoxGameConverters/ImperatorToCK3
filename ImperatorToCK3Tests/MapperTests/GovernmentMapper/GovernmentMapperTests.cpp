#include "../../ImperatorToCK3/Source/Mappers/GovernmentMapper/GovernmentMapper.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_GovernmentMapperTests, nonMatchGivesEmptyOptional)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Government imp = impGovernment }";

	const mappers::GovernmentMapper theMapper(input);

	const auto& ck3Government = theMapper.getCK3GovernmentForImperatorGovernment("nonMatchingGovernment");
	ASSERT_FALSE(ck3Government);
}


TEST(Mappers_GovernmentMapperTests, ck3GovernmentCanBeFound)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Government imp = impGovernment }";

	const mappers::GovernmentMapper theMapper(input);

	const auto& ck3Government = theMapper.getCK3GovernmentForImperatorGovernment("impGovernment");
	ASSERT_EQ("ck3Government", ck3Government);
}


TEST(Mappers_GovernmentMapperTests, multipleImpGovernmentsCanBeInARule)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Government imp = impGovernment imp = impGovernment2 }";

	const mappers::GovernmentMapper theMapper(input);

	const auto& ck3Government = theMapper.getCK3GovernmentForImperatorGovernment("impGovernment2");
	ASSERT_EQ("ck3Government", ck3Government);
}


TEST(Mappers_GovernmentMapperTests, correctRuleMatches)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Government imp = impGovernment }";
	input << "link = { ck3 = ck3Government2 imp = impGovernment2 }";

	const mappers::GovernmentMapper theMapper(input);

	const auto& ck3Government = theMapper.getCK3GovernmentForImperatorGovernment("impGovernment2");
	ASSERT_EQ("ck3Government2", ck3Government);
}