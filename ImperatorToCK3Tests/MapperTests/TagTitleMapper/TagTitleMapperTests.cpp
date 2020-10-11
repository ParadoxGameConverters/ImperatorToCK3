#include "../ImperatorToCK3/Source/Mappers/TagTitleMapper/TagTitleMapper.h"
#include "../ImperatorToCK3/Source/Imperator/Countries/Country.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_TagTitleMapperTests, titleCanBeGenerated)
{
	const mappers::TagTitleMapper theMapper;
	const auto& match = theMapper.getTitleForTag("ROM", ImperatorWorld::countryRankEnum::localPower, "Rome");
	const auto& match2 = theMapper.getTitleForTag("DRE", ImperatorWorld::countryRankEnum::localPower, "Dre Empire");

	ASSERT_EQ("k_IMPTOCK3_ROM", *match);
	ASSERT_EQ("e_IMPTOCK3_DRE", *match2);
}

TEST(Mappers_TagTitleMapperTests, getTitleForTagReturnsNulloptOnEmptyParameter)
{
	const mappers::TagTitleMapper theMapper;
	const auto& match = theMapper.getTitleForTag("", ImperatorWorld::countryRankEnum::migrantHorde, "");

	ASSERT_FALSE(match);
}
