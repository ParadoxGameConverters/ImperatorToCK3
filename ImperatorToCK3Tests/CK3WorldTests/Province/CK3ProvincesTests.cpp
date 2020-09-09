#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/CK3/Province/CK3Provinces.h"


TEST(CK3World_CK3ProvincesTests, provincesDefaltToEmpty)
{
	const CK3::Provinces provinces;

	ASSERT_EQ(0, provinces.getProvinces().size());
}
TEST(CK3World_CK3ProvincesTests, provincesAreProperlyLoadedFromFile)
{
	const CK3::Provinces provinces("TestFiles/CK3ProvincesHistoryFile.txt");

	ASSERT_EQ(4, provinces.getProvinces().size());
	ASSERT_EQ(3080, provinces.getProvinces().find(3080)->first);
	ASSERT_EQ("slovien", provinces.getProvinces().find(3080)->second->getCulture());
	ASSERT_EQ("slavic_pagan", provinces.getProvinces().find(3080)->second->getReligion());
	ASSERT_EQ(4165, provinces.getProvinces().find(4165)->first);
	ASSERT_EQ("slovien", provinces.getProvinces().find(4165)->second->getCulture());
	ASSERT_EQ("slavic_pagan", provinces.getProvinces().find(4165)->second->getReligion());
	ASSERT_EQ(4125, provinces.getProvinces().find(4125)->first);
	ASSERT_EQ("czech", provinces.getProvinces().find(4125)->second->getCulture());
	ASSERT_EQ("slavic_pagan", provinces.getProvinces().find(4125)->second->getReligion());
	ASSERT_EQ(4161, provinces.getProvinces().find(4161)->first);
	ASSERT_EQ("czech", provinces.getProvinces().find(4161)->second->getCulture());
	ASSERT_EQ("slavic_pagan", provinces.getProvinces().find(4161)->second->getReligion());
}