#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/CK3/Province/CK3Province.h"
#include "../ImperatorToCK3/Source/Imperator/Provinces/Province.h"
#include "Mappers/CultureMapper/CultureMapper.h"
#include "Mappers/ReligionMapper/ReligionMapper.h"
#include <sstream>


TEST(CK3World_CK3ProvinceTests, idDefaultsTo0)
{
	const CK3::Province province;

	ASSERT_EQ(0, province.getID());
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
	ASSERT_EQ(42, province.getID());
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

TEST(CK3World_CK3ProvinceTests, holdingDefaultsToNone)
{
	const CK3::Province province;

	ASSERT_EQ("none", province.getHolding());
}
TEST(CK3World_CK3ProvinceTests, holdingCanBeSet)
{
	std::stringstream input{ "= { holding = castle_holding }" };
	CK3::Province province{42, input};
	ASSERT_EQ("castle_holding", province.getHolding());
}


TEST(ImperatorWorld_ProvinceTests, setHoldingLogicWorks)
{
	std::stringstream input{ " = { province_rank=city_metropolis }" };
	std::stringstream input2{ " = { province_rank=city fort=yes }" };
	std::stringstream input3{ " = { province_rank=city }" };
	std::stringstream input4{ " = { province_rank=settlement holy_site = 69 fort=yes }" };
	std::stringstream input5{ " = { province_rank=settlement fort=yes }" };
	std::stringstream input6{ " = { province_rank=settlement }" };

	auto impProvince = std::make_shared<Imperator::Province>(input, 42);
	auto impProvince2 = std::make_shared<Imperator::Province>(input2, 43);
	auto impProvince3 = std::make_shared<Imperator::Province>(input3, 44);
	auto impProvince4 = std::make_shared<Imperator::Province>(input4, 45);
	auto impProvince5 = std::make_shared<Imperator::Province>(input5, 46);
	auto impProvince6 = std::make_shared<Imperator::Province>(input6, 47);

	CK3::Province province, province2, province3, province4, province5, province6;

	mappers::CultureMapper cultureMapper{};
	mappers::ReligionMapper religionMapper{};

	province.initializeFromImperator(impProvince, cultureMapper, religionMapper);
	province2.initializeFromImperator(impProvince2, cultureMapper, religionMapper);
	province3.initializeFromImperator(impProvince3, cultureMapper, religionMapper);
	province4.initializeFromImperator(impProvince4, cultureMapper, religionMapper);
	province5.initializeFromImperator(impProvince5, cultureMapper, religionMapper);
	province6.initializeFromImperator(impProvince6, cultureMapper, religionMapper);

	ASSERT_EQ("city_holding", province.getHolding());
	ASSERT_EQ("castle_holding", province2.getHolding());
	ASSERT_EQ("city_holding", province3.getHolding());
	ASSERT_EQ("church_holding", province4.getHolding());
	ASSERT_EQ("castle_holding", province5.getHolding());
	ASSERT_EQ("tribal_holding", province6.getHolding());
}