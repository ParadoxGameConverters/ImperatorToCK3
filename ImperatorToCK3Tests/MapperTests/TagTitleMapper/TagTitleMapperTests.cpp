#include "../ImperatorToCK3/Source/Mappers/TagTitleMapper/TagTitleMapper.h"
#include "../ImperatorToCK3/Source/Imperator/Countries/Country.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_TagTitleMapperTests, titleCanBeGenerated)
{
	mappers::TagTitleMapper theMapper;
	const auto& match = theMapper.getTitleForTag("ROM", Imperator::countryRankEnum::localPower, "Rome");
	const auto& match2 = theMapper.getTitleForTag("DRE", Imperator::countryRankEnum::localPower, "Dre Empire");

	ASSERT_EQ("k_IMPTOCK3_ROM", *match);
	ASSERT_EQ("e_IMPTOCK3_DRE", *match2);
}

TEST(Mappers_TagTitleMapperTests, getTitleForTagReturnsNulloptOnEmptyParameter)
{
	mappers::TagTitleMapper theMapper;
	const auto& match = theMapper.getTitleForTag("", Imperator::countryRankEnum::migrantHorde, "");

	ASSERT_FALSE(match);
}
