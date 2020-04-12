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

	ASSERT_EQ(families.getFamilies().empty(), true);
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
	const auto& characterItr = families.getFamilies().find(42);
	const auto& characterItr2 = families.getFamilies().find(43);

	ASSERT_EQ(characterItr->first, 42);
	ASSERT_EQ(characterItr->second->getID(), 42);
	ASSERT_EQ(characterItr2->first, 43);
	ASSERT_EQ(characterItr2->second->getID(), 43);
}