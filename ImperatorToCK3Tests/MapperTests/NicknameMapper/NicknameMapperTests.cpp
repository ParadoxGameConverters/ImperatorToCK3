#include "../../ImperatorToCK3/Source/Mappers/NicknameMapper/NicknameMapper.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_NicknameMapperTests, nonMatchGivesEmptyOptional)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Nickname imp = impNickname }";

	const mappers::NicknameMapper theMapper(input);

	const auto& ck3Nickname = theMapper.getCK3NicknameForImperatorNickname("nonMatchingNickname");
	ASSERT_FALSE(ck3Nickname);
}


TEST(Mappers_NicknameMapperTests, ck3NicknameCanBeFound)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Nickname imp = impNickname }";

	const mappers::NicknameMapper theMapper(input);

	const auto& ck3Nickname = theMapper.getCK3NicknameForImperatorNickname("impNickname");
	ASSERT_EQ("ck3Nickname", ck3Nickname);
}


TEST(Mappers_NicknameMapperTests, multipleImpNicknamesCanBeInARule)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Nickname imp = impNickname imp = impNickname2 }";

	const mappers::NicknameMapper theMapper(input);

	const auto& ck3Nickname = theMapper.getCK3NicknameForImperatorNickname("impNickname2");
	ASSERT_EQ("ck3Nickname", ck3Nickname);
}


TEST(Mappers_NicknameMapperTests, correctRuleMatches)
{
	std::stringstream input;
	input << "link = { ck3 = ck3Nickname imp = impNickname }";
	input << "link = { ck3 = ck3Nickname2 imp = impNickname2 }";

	const mappers::NicknameMapper theMapper(input);

	const auto& ck3Nickname = theMapper.getCK3NicknameForImperatorNickname("impNickname2");
	ASSERT_EQ("ck3Nickname2", ck3Nickname);
}