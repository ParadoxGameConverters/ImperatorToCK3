#include "../ImperatorToCK3/Source/Mappers/TagTitleMapper/TagTitleMapping.h"
#include "gtest/gtest.h"
#include <sstream>


TEST(Mappers_TagTitleMappingTests, simpleTagMatch)
{
	std::stringstream mappingStream;
	mappingStream << "{ ck3 = e_roman_empire imp = ROM }";

	mappers::TagTitleMapping theMapping(mappingStream);
	const auto& match = theMapping.tagRankMatch("ROM", "");

	ASSERT_EQ("e_roman_empire", *match);
}

TEST(Mappers_TagTitleMappingTests, simpleTagMatchFailsOnWrongTag)
{
	std::stringstream mappingStream;
	mappingStream << "{ ck3 = e_roman_empire imp = REM }";

	mappers::TagTitleMapping theMapping(mappingStream);
	const auto& match = theMapping.tagRankMatch("ROM", "");

	ASSERT_EQ(std::nullopt, match);
}

TEST(Mappers_TagTitleMappingTests, simpleTagMatchFailsOnNoTag)
{
	std::stringstream mappingStream;
	mappingStream << "{ ck3 = e_roman_empire }";

	mappers::TagTitleMapping theMapping(mappingStream);
	const auto& match = theMapping.tagRankMatch("ROM", "");

	ASSERT_EQ(std::nullopt, match);
}

TEST(Mappers_TagTitleMappingTests, tagRankMatch)
{
	std::stringstream mappingStream;
	mappingStream << "{ ck3 = e_roman_empire imp = ROM rank = e }";

	mappers::TagTitleMapping theMapping(mappingStream);
	const auto& match = theMapping.tagRankMatch("ROM", "e");

	ASSERT_EQ("e_roman_empire", *match);
}

TEST(Mappers_TagTitleMappingTests, tagRankMatchFailsOnWrongRank)
{
	std::stringstream mappingStream;
	mappingStream << "{ ck3 = e_roman_empire imp = ROM rank = k }";

	mappers::TagTitleMapping theMapping(mappingStream);
	const auto& match = theMapping.tagRankMatch("ROM", "e");

	ASSERT_EQ(std::nullopt, match);
}

TEST(Mappers_TagTitleMappingTests, tagRankMatchSucceedsOnNoRank)
{
	std::stringstream mappingStream;
	mappingStream << "{ ck3 = e_roman_empire imp = ROM }";

	mappers::TagTitleMapping theMapping(mappingStream);
	const auto& match = theMapping.tagRankMatch("ROM", "e");

	ASSERT_EQ("e_roman_empire", *match);
}
