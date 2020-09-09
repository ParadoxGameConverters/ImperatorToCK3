#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/Imperator/Provinces/Province.h"
#include "../commonItems/Date.h"
#include <sstream>

TEST(ImperatorWorld_ProvinceTests, IDCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Province theProvince(input, 42);

	ASSERT_EQ(42, theProvince.getID());
}
TEST(ImperatorWorld_ProvinceTests, cultureCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tculture=\"paradoxian\"";
	input << "}";

	const ImperatorWorld::Province theProvince(input, 42);

	ASSERT_EQ("paradoxian", theProvince.getCulture());
}

TEST(ImperatorWorld_ProvinceTests, cultureDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Province theProvince(input, 42);

	ASSERT_TRUE(theProvince.getCulture().empty());
}


TEST(ImperatorWorld_ProvinceTests, religionCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\treligion=\"paradoxian\"";
	input << "}";

	const ImperatorWorld::Province theProvince(input, 42);

	ASSERT_EQ("paradoxian", theProvince.getReligion());
}

TEST(ImperatorWorld_ProvinceTests, religionDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Province theProvince(input, 42);

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

	const ImperatorWorld::Province theProvince(input, 42);

	ASSERT_EQ("Biggus Dickus", theProvince.getName());
}

TEST(ImperatorWorld_ProvinceTests, nameDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Province theProvince(input, 42);

	ASSERT_TRUE(theProvince.getName().empty());
}

TEST(ImperatorWorld_ProvinceTests, ownerCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\towner=69\n";
	input << "}";

	const ImperatorWorld::Province theProvince(input, 42);

	ASSERT_EQ(69, theProvince.getOwner());
}

TEST(ImperatorWorld_ProvinceTests, ownerDefaultsTo0)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Province theProvince(input, 42);

	ASSERT_EQ(0, theProvince.getOwner());
}

TEST(ImperatorWorld_ProvinceTests, controllerCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tcontroller=69\n";
	input << "}";

	const ImperatorWorld::Province theProvince(input, 42);

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

	const ImperatorWorld::Province theProvince(input, 42);

	ASSERT_EQ(4, theProvince.getPopCount());
}

TEST(ImperatorWorld_ProvinceTests, buildingsCountCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tbuildings = {0 1 0 65 3}\n";
	input << "}";

	const ImperatorWorld::Province theProvince(input, 42);

	ASSERT_EQ(69, theProvince.getBuildingsCount());
}

TEST(ImperatorWorld_ProvinceTests, buildingsCountDefaultsTo0)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Province theProvince(input, 42);

	ASSERT_EQ(0, theProvince.getBuildingsCount());
}
