#include "../../ImperatorToCK3/Source/CK3/Province/CK3Province.h"
#include "../../ImperatorToCK3/Source/Mappers/RegionMapper/County.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_AreaTests, blankCountiesLoadForNoProvinces)
{
	std::stringstream input;

	const mappers::County newCounty(input);

	ASSERT_TRUE(newCounty.getProvinces().empty());
}

TEST(Mappers_AreaTests, provincesCanBeLoaded)
{
	std::stringstream input;
	input << "{ b_cringe = { province = 6 } b_newbarony2 = { province = 4 }  b_newbarony3 = { province = 69 } }";

	const mappers::County newCounty(input);

	ASSERT_FALSE(newCounty.getProvinces().empty());
	ASSERT_EQ(newCounty.getProvinces().size(), 3);
	ASSERT_EQ(newCounty.getProvinces().find(6)->first, 6);
	ASSERT_EQ(newCounty.getProvinces().find(4)->first, 4);
	ASSERT_EQ(newCounty.getProvinces().find(69)->first, 69);
}

TEST(Mappers_AreaTests, provincesCanBeFound)
{
	std::stringstream input;
	input << "{ b_cringe = { province = 6 } b_newbarony2 = { province = 4 }  b_newbarony3 = { province = 69 } }";

	const mappers::County newCounty(input);

	ASSERT_TRUE(newCounty.countyContainsProvince(6));
	ASSERT_TRUE(newCounty.countyContainsProvince(4));
	ASSERT_TRUE(newCounty.countyContainsProvince(69));
}

TEST(Mappers_AreaTests, provinceMismatchReturnsFalse)
{
	std::stringstream input;
	input << "{ b_cringe = { province = 6 } b_newbarony2 = { province = 4 }  b_newbarony3 = { province = 69 } }";

	const mappers::County newArea(input);

	ASSERT_FALSE(newArea.countyContainsProvince(7));
}

TEST(Mappers_AreaTests, provinceCanLinkToCK3Province)
{
	std::stringstream input;
	input << "{ b_cringe = { province = 6 } b_newbarony2 = { province = 4 }  b_newbarony3 = { province = 69 } }";
	mappers::County newCounty(input);

	auto eu4Province = std::make_shared<CK3::Province>();

	ASSERT_FALSE(newCounty.getProvinces().find(69)->second);
	newCounty.linkProvince(std::pair(69, eu4Province));
	ASSERT_TRUE(newCounty.getProvinces().find(69)->second);
}
