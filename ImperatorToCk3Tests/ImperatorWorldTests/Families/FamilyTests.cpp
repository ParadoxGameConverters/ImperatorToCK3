#include "gtest/gtest.h"
#include "../ImperatorToCk3/Source/Imperator/Families/Family.h"
#include <sstream>

TEST(ImperatorWorld_FamilyTests, IDCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ(theFamily.getID(), 42);
}

TEST(ImperatorWorld_FamilyTests, cultureCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tculture=\"paradoxian\"";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ(theFamily.getCulture(), "paradoxian");
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
	input << "\prestige=\"420.5\"";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ(theFamily.getPrestige(), 420.5);
}

TEST(ImperatorWorld_FamilyTests, prestigeDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ(theFamily.getPrestige(), 0);
}

TEST(ImperatorWorld_FamilyTests, prestigeRatioCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\prestige_ratio=\"0.75\"";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ(theFamily.getPrestigeRatio(), 0.75);
}

TEST(ImperatorWorld_FamilyTests, prestigeRatioDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ(theFamily.getPrestigeRatio(), 0);
}

TEST(ImperatorWorld_FamilyTests, keyCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tkey=\"paradoxian\"";
	input << "}";

	const ImperatorWorld::Family theFamily(input, 42);

	ASSERT_EQ(theFamily.getKey(), "paradoxian");
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
