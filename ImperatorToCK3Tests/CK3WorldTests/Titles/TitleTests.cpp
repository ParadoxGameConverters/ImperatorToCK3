#include "../ImperatorToCK3/Source/CK3/Titles/Title.h"
#include "../ImperatorToCK3/Source/Mappers/LocalizationMapper/LocalizationMapper.h"
#include "../ImperatorToCK3/Source/Mappers/CoaMapper/CoaMapper.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(CK3World_TitleTests, localizationCanBeSet)
{
	CK3::Title theTitle;
	const mappers::LocBlock locBlock = { "engloc", "frloc", "germloc", "rusloc", "spaloc" };

	theTitle.setLocalizations(locBlock);
	ASSERT_EQ(1, theTitle.getLocalizations().size());
}

TEST(CK3World_TitleTests, membersDefaultToBlank)
{
	std::stringstream input;
	const CK3::Title theTitle;

	ASSERT_TRUE(theTitle.getTitleName().empty());
	ASSERT_TRUE(theTitle.getHistoryCountryFile().empty());
	ASSERT_TRUE(theTitle.getLocalizations().empty());
	ASSERT_FALSE(theTitle.getCoa());
	ASSERT_FALSE(theTitle.getCapitalCounty());
}
