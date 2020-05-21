#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/Imperator/Families/Family.h"
#include <sstream>

TEST(ImperatorWorld_FamilyTests, IDCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ(42, theFamily.getID());
}

TEST(ImperatorWorld_FamilyTests, cultureCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tculture=\"paradoxian\"";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ("paradoxian", theFamily.getCulture());
}

TEST(ImperatorWorld_FamilyTests, cultureDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_TRUE(theFamily.getCulture().empty());
}

TEST(ImperatorWorld_FamilyTests, prestigeCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tprestige=\"420.5\"";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ(420.5, theFamily.getPrestige());
}

TEST(ImperatorWorld_FamilyTests, prestigeDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ(0, theFamily.getPrestige());
}

TEST(ImperatorWorld_FamilyTests, prestigeRatioCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tprestige_ratio=\"0.75\"";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ(0.75, theFamily.getPrestigeRatio());
}

TEST(ImperatorWorld_FamilyTests, prestigeRatioDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ(0, theFamily.getPrestigeRatio());
}

TEST(ImperatorWorld_FamilyTests, keyCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tkey=\"paradoxian\"";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ("paradoxian", theFamily.getKey());
}

TEST(ImperatorWorld_FamilyTests, keyDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_TRUE(theFamily.getKey().empty());
}
