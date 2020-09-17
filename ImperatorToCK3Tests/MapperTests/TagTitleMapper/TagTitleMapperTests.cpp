#include "../ImperatorToCK3/Source/Mappers/TagTitleMapper/TagTitleMapper.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_TagTitleMapperTests, titleCanBeGenerated)
{
	mappers::TagTitleMapper theMapper;
	const auto& match = theMapper.getTitleForTag("ROM");
	const auto& match2 = theMapper.getTitleForTag("DRE");

	ASSERT_EQ("e_IMPTOCK3_ROM", *match);
	ASSERT_EQ("e_IMPTOCK3_DRE", *match2);
}
