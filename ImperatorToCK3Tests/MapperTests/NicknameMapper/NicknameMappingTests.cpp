#include "../../ImperatorToCK3/Source/Mappers/NicknameMapper/NicknameMapping.h"
#include "gtest/gtest.h"
#include <sstream>


TEST(Mappers_NicknameMappingTests, ck3NicknameDefaultsToNullopt)
{
	std::stringstream input;
	input << "= {}";

	const mappers::NicknameMapping theMapping(input);

	ASSERT_FALSE(theMapping.ck3Nickname);
}


TEST(Mappers_NicknameMappingTests, ck3NicknameCanBeSet)
{
	std::stringstream input;
	input << "= { ck3 = ck3Nickname }";

	const mappers::NicknameMapping theMapping(input);

	ASSERT_EQ("ck3Nickname", theMapping.ck3Nickname);
}


TEST(Mappers_NicknameMappingTests, imperatorNicknamesDefaultToEmpty)
{
	std::stringstream input;
	input << "= {}";

	const mappers::NicknameMapping theMapping(input);

	ASSERT_TRUE(theMapping.impNicknames.empty());
}


TEST(Mappers_NicknameMappingTests, imperatorNicknamesCanBeSet)
{
	std::stringstream input;
	input << "= { imp = nickname1 imp = nickname2 }";

	const mappers::NicknameMapping theMapping(input);

	ASSERT_EQ(2, theMapping.impNicknames.size());
	ASSERT_EQ("nickname1", *theMapping.impNicknames.find("nickname1"));
	ASSERT_EQ("nickname2", *theMapping.impNicknames.find("nickname2"));
}