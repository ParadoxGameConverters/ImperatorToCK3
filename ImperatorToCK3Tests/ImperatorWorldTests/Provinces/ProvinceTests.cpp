#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/Imperator/Provinces/Province.h"
#include "../ImperatorToCK3/Source/Imperator/Provinces/ProvinceFactory.h"
#include "../commonItems/Date.h"
#include <sstream>

TEST(ImperatorWorld_ProvinceTests, IDCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_EQ(42, theProvince.getID());
}
TEST(ImperatorWorld_ProvinceTests, cultureCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tculture=\"paradoxian\"";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_EQ("paradoxian", theProvince.getCulture());
}

TEST(ImperatorWorld_ProvinceTests, cultureDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_TRUE(theProvince.getCulture().empty());
}


TEST(ImperatorWorld_ProvinceTests, religionCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\treligion=\"paradoxian\"";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_EQ("paradoxian", theProvince.getReligion());
}

TEST(ImperatorWorld_ProvinceTests, religionDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_TRUE(theProvince.getReligion().empty());
}

TEST(ImperatorWorld_ProvinceTests, nameCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "province_name = {\n";
	input << "name=\"Biggus Dickus\"\n";
	input << "}\n";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_EQ("Biggus Dickus", theProvince.getName());
}

TEST(ImperatorWorld_ProvinceTests, nameDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_TRUE(theProvince.getName().empty());
}

TEST(ImperatorWorld_ProvinceTests, ownerCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\towner=69\n";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_EQ(69, theProvince.getOwner());
}

TEST(ImperatorWorld_ProvinceTests, ownerDefaultsTo0)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_EQ(0, theProvince.getOwner());
}

TEST(ImperatorWorld_ProvinceTests, controllerCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tcontroller=69\n";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_EQ(69, theProvince.getController());
}

TEST(ImperatorWorld_ProvinceTests, popsCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tpop=69\n";
	input << "\tpop=68\n";
	input << "\tpop=12213\n";
	input << "\tpop=23\n";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_EQ(4, theProvince.getPopCount());
}

TEST(ImperatorWorld_ProvinceTests, province_rankDefaultsToSettlement)
{
	std::stringstream input;
	const auto province = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_EQ(Imperator::ProvinceRank::settlement, province.getProvinceRank());
}

TEST(ImperatorWorld_ProvinceTests, province_rankCanBeSet)
{
	std::stringstream input{ "= { province_rank=settelement }" };
	std::stringstream input2{ "= { province_rank=city }" };
	std::stringstream input3{ "= { province_rank=city_metropolis }" };

	auto provinceFactory = Imperator::Province::Factory();
	const auto province = *provinceFactory.getProvince(input, 42);
	const auto province2 = *provinceFactory.getProvince(input2, 43);
	const auto province3 = *provinceFactory.getProvince(input3, 44);

	ASSERT_EQ(Imperator::ProvinceRank::settlement, province.getProvinceRank());
	ASSERT_EQ(Imperator::ProvinceRank::city, province2.getProvinceRank());
	ASSERT_EQ(Imperator::ProvinceRank::city_metropolis, province3.getProvinceRank());
}

TEST(ImperatorWorld_ProvinceTests, fortDefaultsToFalse)
{
	std::stringstream input;
	const auto province = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_FALSE(province.hasFort());
}

TEST(ImperatorWorld_ProvinceTests, fortCanBeSet)
{
	std::stringstream input{" = { fort=yes }"};
	const auto province = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_TRUE(province.hasFort());
}

TEST(ImperatorWorld_ProvinceTests, holySiteDefaultsToFalse)
{
	std::stringstream input;
	const auto province = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_FALSE(province.isHolySite());
}

TEST(ImperatorWorld_ProvinceTests, holySiteCanBeSet)
{
	std::stringstream input{ " = { holy_site=4294967295 }" }; // this value means no holy site
	std::stringstream input2{ " = { holy_site=56 }" };
	auto provinceFactory = Imperator::Province::Factory();
	const auto province = *provinceFactory.getProvince(input, 42);
	const auto province2 = *provinceFactory.getProvince(input2, 43);

	ASSERT_FALSE(province.isHolySite());
	ASSERT_TRUE(province2.isHolySite());
}

TEST(ImperatorWorld_ProvinceTests, buildingsCountCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tbuildings = {0 1 0 65 3}\n";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_EQ(69, theProvince.getBuildingsCount());
}

TEST(ImperatorWorld_ProvinceTests, buildingsCountDefaultsTo0)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto theProvince = *Imperator::Province::Factory().getProvince(input, 42);

	ASSERT_EQ(0, theProvince.getBuildingsCount());
}
