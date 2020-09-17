#include "../ImperatorToCK3/Source/Mappers/TagTitleMapper/TagTitleMapper.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_TagTitleMapperTests, titleCanBeGenerated)
{
	mappers::TagTitleMapper theMapper;
	const auto& match = theMapper.getTitleForTag("ROM");

	ASSERT_EQ("e_IMPTOCK3_ROM", *match);
}
