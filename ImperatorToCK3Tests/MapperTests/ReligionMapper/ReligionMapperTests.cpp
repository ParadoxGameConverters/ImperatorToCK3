#include "../../ImperatorToCK3/Source/Mappers/ReligionMapper/ReligionMapper.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_ReligionMapperTests, nonMatchGivesEmptyOptional)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Religion imp = impReligion }";

	const mappers::ReligionMapper theMapper(input);

	const auto& eu4Religion = theMapper.getCK3ReligionForImperatorReligion("nonMatchingReligion");
	ASSERT_FALSE(eu4Religion);
}


TEST(Mappers_ReligionMapperTests, eu4ReligionCanBeFound)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Religion imp = impReligion }";

	const mappers::ReligionMapper theMapper(input);

	const auto& eu4Religion = theMapper.getCK3ReligionForImperatorReligion("impReligion");
	ASSERT_EQ(eu4Religion, "ck3Religion");
}


TEST(Mappers_ReligionMapperTests, multipleEU4ReligionsCanBeInARule)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Religion imp = impReligion imp = impReligion2 }";

	const mappers::ReligionMapper theMapper(input);

	const auto& eu4Religion = theMapper.getCK3ReligionForImperatorReligion("impReligion2");
	ASSERT_EQ(eu4Religion, "ck3Religion");
}


TEST(Mappers_ReligionMapperTests, correctRuleMatches)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Religion imp = impReligion }";
	input << "link = { ck3 = ck3Religion2 imp = impReligion2 }";

	const mappers::ReligionMapper theMapper(input);

	const auto& eu4Religion = theMapper.getCK3ReligionForImperatorReligion("impReligion2");
	ASSERT_EQ(eu4Religion, "ck3Religion2");
}