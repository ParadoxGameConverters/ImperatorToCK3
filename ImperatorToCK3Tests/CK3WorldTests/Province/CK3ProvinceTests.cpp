#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/CK3/Province/CK3Province.h"
#include <sstream>


TEST(CK3World_CK3ProvinceTests, idDefaultsTo0)
{
	const CK3::Province province;

	ASSERT_EQ(0, province.getProvinceID());
}
TEST(CK3World_CK3ProvinceTests, religionDefaultsToEmpty)
{
	const CK3::Province province;

	ASSERT_EQ("", province.getReligion());
}
TEST(CK3World_CK3ProvinceTests, cultureDefaultsToEmpty)
{
	const CK3::Province province;

	ASSERT_EQ("", province.getCulture());
}
TEST(CK3World_CK3ProvinceTests, religionCanBeSet)
{
	CK3::Province province;
	province.setReligion("religion");
	ASSERT_EQ("religion", province.getReligion());
}
TEST(CK3World_CK3ProvinceTests, provinceCanBeLoadedFromStream)
{
	std::stringstream input;
	input << "{ culture=roman random_key=random_value religion=orthodox }";
	CK3::Province province(42, input);
	ASSERT_EQ(42, province.getProvinceID());
	ASSERT_EQ("orthodox", province.getReligion());
	ASSERT_EQ("roman", province.getCulture());
}
TEST(CK3World_CK3ProvinceTests, provinceCanBeUpdatedFromFile)
{
	std::stringstream input;
	input << "{ culture=dfgdfgdfg random_key=random_value religion=ertert }";
	CK3::Province province(42, input);
	province.updateWith("TestFiles/CK3ProvinceDetails/CK3ProvinceDetailsCorrect.txt");
	ASSERT_EQ("orthodox", province.getReligion());
	ASSERT_EQ("roman", province.getCulture());
}
