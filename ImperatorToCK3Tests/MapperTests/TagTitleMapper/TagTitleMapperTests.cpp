#include "../ImperatorToCK3/Source/Mappers/TagTitleMapper/TagTitleMapper.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_TitleTagMapperTests, emptyMappingsDefaultToEmpty)
{
	const mappers::TagTitleMapper theMapper;

	ASSERT_TRUE(theMapper.getRegisteredTitleTags().empty());
}

TEST(Mappers_TitleTagMapperTests, titleCanBeGenerated)
{
	mappers::TagTitleMapper theMapper;
	const auto& match = theMapper.getTitleForTag("ROM");

	ASSERT_EQ("e_IMPTOCK3_ROM", *match);
}
