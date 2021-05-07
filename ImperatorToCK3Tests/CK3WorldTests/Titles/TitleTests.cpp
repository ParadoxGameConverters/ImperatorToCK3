#include "gtest/gtest.h"
#include "CK3/Character/CK3Character.h"
#include "CK3/Titles/Title.h"
#include "CK3/Titles/LandedTitles.h"
#include "CK3/Titles/TitlesHistory.h"
#include "Mappers/LocalizationMapper/LocalizationMapper.h"
#include "Mappers/CoaMapper/CoaMapper.h"
#include <sstream>



TEST(CK3World_TitleTests, titlePrimitivesDefaultToBlank) {
	std::stringstream input;
	CK3::Title title;
	title.loadTitles(input);

	ASSERT_FALSE(title.hasDefiniteForm());
	ASSERT_FALSE(title.isLandless());
	ASSERT_FALSE(title.getColor());
	ASSERT_FALSE(title.getCapitalCounty()->second);
	ASSERT_FALSE(title.getProvince());
}

TEST(CK3World_TitleTests, titlePrimitivesCanBeLoaded) {
	std::stringstream input;
	input << "definite_form = yes\n";
	input << "landless = yes\n";
	input << "color = { 23 23 23 }\n";
	input << "capital = c_roma\n";
	input << "province = 345\n";

	CK3::Title title;
	title.loadTitles(input);

	ASSERT_TRUE(title.hasDefiniteForm());
	ASSERT_TRUE(title.isLandless());
	ASSERT_EQ("= rgb { 23 23 23 }", title.getColor()->outputRgb());
	ASSERT_EQ("c_roma", title.getCapitalCounty()->first);
	ASSERT_EQ(345, title.getProvince());
}

TEST(CK3World_TitleTests, localizationCanBeSet) {
	CK3::Title theTitle;
	const mappers::LocBlock locBlock = { "engloc", "frloc", "germloc", "rusloc", "spaloc" };

	theTitle.setLocalizations(locBlock);
	ASSERT_EQ(1, theTitle.getLocalizations().size());
}

TEST(CK3World_TitleTests, membersDefaultToBlank) {
	std::stringstream input;
	const CK3::Title theTitle;

	ASSERT_TRUE(theTitle.getName().empty());
	ASSERT_TRUE(theTitle.getLocalizations().empty());
	ASSERT_FALSE(theTitle.getCoA());
	ASSERT_FALSE(theTitle.getCapitalCounty());
}

TEST(CK3World_TitleTests, holderDefaultsTo0String) {
	std::stringstream input;
	const CK3::Title theTitle;

	ASSERT_EQ("0", theTitle.getHolder()->ID);
}

TEST(CK3World_TitleTests, capitalBaronyDefaultsToNullopt) {
	std::stringstream input;
	const CK3::Title theTitle;

	ASSERT_FALSE(theTitle.capitalBaronyProvince);
}


TEST(CK3World_TitleTests, historyCanBeAdded) {
	CK3::TitlesHistory titlesHistory("TestFiles/title_history");
	const CK3::TitleHistory history = *titlesHistory.popTitleHistory("k_greece");
	CK3::Title title;
	title.addHistory(CK3::LandedTitles{}, history);

	ASSERT_EQ("420", title.getHolder()->ID);
	ASSERT_EQ(20, *title.getDevelopmentLevel());
}


TEST(CK3World_TitleTests, developmentLevelCanBeInherited) {
	auto vassalPtr = std::make_shared<CK3::Title>("c_vassal");
	auto liegePtr = std::make_shared<CK3::Title>("d_liege");
	liegePtr->setDevelopmentLevel(8);
	vassalPtr->setDeJureLiege(liegePtr);

	ASSERT_EQ(8, *vassalPtr->getOwnOrInheritedDevelopmentLevel());
}


TEST(CK3World_TitleTests, inheritedDevelopmentCanBeNullopt) {
	auto vassalPtr = std::make_shared<CK3::Title>("c_vassal");
	auto liegePtr = std::make_shared<CK3::Title>("d_liege");
	vassalPtr->setDeJureLiege(liegePtr);

	ASSERT_EQ(std::nullopt, vassalPtr->getOwnOrInheritedDevelopmentLevel());
}
