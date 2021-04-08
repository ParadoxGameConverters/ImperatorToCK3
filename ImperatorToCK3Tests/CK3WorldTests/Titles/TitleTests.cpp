#include "gtest/gtest.h"
#include "CK3/Titles/Title.h"
#include "Mappers/LocalizationMapper/LocalizationMapper.h"
#include "Mappers/CoaMapper/CoaMapper.h"
#include <sstream>



TEST(CK3World_TitleTests, titlePrimitivesDefaultToBlank) {
	std::stringstream input;
	CK3::Title title;
	title.loadTitles(input);

	ASSERT_FALSE(title.definiteForm);
	ASSERT_FALSE(title.landless);
	ASSERT_FALSE(title.color);
	ASSERT_FALSE(title.capital.second);
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

	ASSERT_TRUE(title.definiteForm);
	ASSERT_TRUE(title.landless);
	ASSERT_EQ("= rgb { 23 23 23 }", title.color->outputRgb());
	ASSERT_EQ("c_roma", title.capital.first);
	ASSERT_EQ(345, title.getProvince());
}

TEST(CK3World_TitleTests, localizationCanBeSet) {
	CK3::Title theTitle;
	const mappers::LocBlock locBlock = { "engloc", "frloc", "germloc", "rusloc", "spaloc" };

	theTitle.setLocalizations(locBlock);
	ASSERT_EQ(1, theTitle.localizations.size());
}

TEST(CK3World_TitleTests, membersDefaultToBlank) {
	std::stringstream input;
	const CK3::Title theTitle;

	ASSERT_TRUE(theTitle.getName().empty());
	ASSERT_TRUE(theTitle.localizations.empty());
	ASSERT_FALSE(theTitle.coa);
	ASSERT_FALSE(theTitle.capitalCounty);
}

TEST(CK3World_TitleTests, holderDefaultsTo0String) {
	std::stringstream input;
	const CK3::Title theTitle;

	ASSERT_EQ("0", theTitle.holder);
}

TEST(CK3World_TitleTests, capitalBaronyDefaultsToNullopt) {
	std::stringstream input;
	const CK3::Title theTitle = {};

	ASSERT_FALSE(theTitle.capitalBaronyProvince);
}