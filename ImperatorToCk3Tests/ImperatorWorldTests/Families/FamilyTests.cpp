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

TEST(ImperatorWorld_FamilyTests, keyCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tname=\"paradoxian\"";
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
