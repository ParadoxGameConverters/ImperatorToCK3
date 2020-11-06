#include "../../ImperatorToCK3/Source/CK3/Titles/LandedTitles.h"
#include "../../ImperatorToCK3/Source/CK3/Titles/Title.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(CK3World_LandedTitlesTests, titlesDefaultToEmpty)
{
	std::stringstream input;
	CK3::LandedTitles titles;
	titles.loadTitles(input);

	ASSERT_TRUE(titles.getTitles().empty());
}

TEST(CK3World_LandedTitlesTests, titlesCanBeLoaded)
{
	std::stringstream input;
	input << "b_barony = { province = 12 }\n";
	input << "c_county = { landless = yes }\n";

	CK3::LandedTitles titles;
	titles.loadTitles(input);

	const auto& barony = titles.getTitles().find("b_barony");
	const auto& county = titles.getTitles().find("c_county");

	ASSERT_EQ(2, titles.getTitles().size());
	ASSERT_EQ(12, barony->second->getProvince());
	ASSERT_TRUE(county->second->landless);
}

TEST(CK3World_LandedTitlesTests, titlesCanBeLoadedRecursively)
{
	std::stringstream input;
	input << "e_empire1 = { k_kingdom2 = { d_duchy3 = { b_barony4 = { province = 12 } } } }\n";
	input << "c_county5 = { landless = yes }\n";

	CK3::LandedTitles titles;
	titles.loadTitles(input);

	const auto& barony = titles.getTitles().find("b_barony4");
	const auto& county = titles.getTitles().find("c_county5");

	ASSERT_EQ(5, titles.getTitles().size());
	ASSERT_EQ(12, barony->second->getProvince());
	ASSERT_TRUE(county->second->landless);
}

TEST(CK3World_LandedTitlesTests, titlesCanBeOverriddenByMods)
{
	std::stringstream input;
	input << "e_empire1 = { k_kingdom2 = { d_duchy3 = { b_barony4 = { province = 12 } } } }\n";
	input << "c_county5 = { landless = yes }\n";

	CK3::LandedTitles titles;
	titles.loadTitles(input);

	std::stringstream input2;
	input2 << "b_barony4 = { province = 15 }\n";
	input2 << "c_county5 = { landless = NO }\n";
	titles.loadTitles(input2);

	const auto& barony = titles.getTitles().find("b_barony4");
	const auto& county = titles.getTitles().find("c_county5");

	ASSERT_EQ(5, titles.getTitles().size());
	ASSERT_EQ(15, barony->second->getProvince());
	ASSERT_FALSE(county->second->landless);
}

TEST(CK3World_LandedTitlesTests, titlesCanBeAddedByMods)
{
	std::stringstream input;
	input << "e_empire1 = { k_kingdom2 = { d_duchy3 = { b_barony4 = { province = 12 } } } }\n";
	input << "c_county5 = { landless = yes }\n";

	CK3::LandedTitles titles;
	titles.loadTitles(input);

	std::stringstream input2;
	input2 << "c_county5 = { landless = NO }\n"; // Overrides existing instance
	input2 << "e_empire6 = { k_kingdom7 = { d_duchy8 = { b_barony9 = { province = 12 } } } }\n";
	input2 << "c_county10 = { landless = yes }\n";
	titles.loadTitles(input2);

	ASSERT_EQ(10, titles.getTitles().size());
}


