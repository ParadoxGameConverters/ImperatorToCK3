#include "gtest/gtest.h"
#include "CK3/Titles/TitlesHistory.h"
#include <sstream>



TEST(CK3World_CK3TitleHistoryTests, holderDefaultsToZeroString) {
	const CK3::TitleHistory history;

	ASSERT_EQ("0", history.holder);
}


TEST(CK3World_CK3TitleHistoryTests, liegeDefaultsToNullopt) {
	const CK3::TitleHistory history;

	ASSERT_EQ(std::nullopt, history.liege);
}


TEST(CK3World_CK3TitleHistoryTests, governmentDefaultsToNullopt) {
	const CK3::TitleHistory history;

	ASSERT_EQ(std::nullopt, history.government);
}


TEST(CK3World_CK3TitleHistoryTests, developmentLevelDefaultsToNullopt) {
	const CK3::TitleHistory history;

	ASSERT_EQ(std::nullopt, history.developmentLevel);
}


TEST(CK3World_CK3TitleHistoryTests, historyCanBeLoadedFromStream) {
	CK3::TitlesHistory titlesHistory("TestFiles/title_history");
	const CK3::TitleHistory history = *titlesHistory.popTitleHistory("k_rome");

	ASSERT_EQ("67", history.holder);
	ASSERT_EQ("e_italia", *history.liege);
}


TEST(CK3World_CK3TitleHistoryTests, detailsAreLoadedFromDatedBlocks) {
	CK3::TitlesHistory titlesHistory("TestFiles/title_history");
	const CK3::TitleHistory history = *titlesHistory.popTitleHistory("k_greece");

	ASSERT_EQ("420", history.holder);
	ASSERT_EQ(20, *history.developmentLevel);
}
