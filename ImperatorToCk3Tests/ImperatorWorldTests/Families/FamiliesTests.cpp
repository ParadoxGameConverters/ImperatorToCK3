#include "gtest/gtest.h"
#include <sstream>

#include "../ImperatorToCk3/Source/Imperator/Families/Families.h"
#include "../ImperatorToCk3/Source/Imperator/Families/Family.h"

TEST(ImperatorWorld_FamiliesTests, familiesDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Families families(input);

	ASSERT_TRUE(families.getFamilies().empty());
}

TEST(ImperatorWorld_FamiliesTests, familiesCanBeLoaded)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "42={}\n";
	input << "43={}\n";
	input << "}";

	const ImperatorWorld::Families families(input);
	const auto& familyItr = families.getFamilies().find(42);
	const auto& familyItr2 = families.getFamilies().find(43);

	ASSERT_EQ(42, familyItr->first);
	ASSERT_EQ(42, familyItr->second->getID());
	ASSERT_EQ(43, familyItr2->first);
	ASSERT_EQ(43, familyItr2->second->getID());
}