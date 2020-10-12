#include "gtest/gtest.h"
#include <sstream>

#include "../ImperatorToCK3/Source/Imperator/Families/Families.h"
#include "../ImperatorToCK3/Source/Imperator/Families/Family.h"

TEST(ImperatorWorld_FamiliesTests, familiesDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	Imperator::Families families;
	families.loadFamilies(input);

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

	Imperator::Families families;
	families.loadFamilies(input);
	const auto& familyItr = families.getFamilies().find(42);
	const auto& familyItr2 = families.getFamilies().find(43);

	ASSERT_EQ(42, familyItr->first);
	ASSERT_EQ(42, familyItr->second->getID());
	ASSERT_EQ(43, familyItr2->first);
	ASSERT_EQ(43, familyItr2->second->getID());
}

TEST(ImperatorWorld_FamiliesTests, familiesCanBeUpdated)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "42={\n";
	input << "key = \"Losers\"\n";
	input << "culture = arabic\n";
	input << "}\n";
	input << "42={\n"; /// same ID as before, intended here
	input << "key = \"Chads\"\n";
	input << "culture = roman\n";
	input << "}\n";
	input << "}";

	Imperator::Families families;
	families.loadFamilies(input);
	const auto& familyItr = families.getFamilies().find(42);
	ASSERT_EQ("Chads", familyItr->second->getKey());
	ASSERT_EQ("roman", familyItr->second->getCulture());
}

TEST(ImperatorWorld_FamiliesTests, literalNoneFamiliesAreNotLoaded)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "42=none\n";
	input << "43={}\n";
	input << "44=none\n";
	input << "}";

	Imperator::Families families;
	families.loadFamilies(input);
	const auto& familyItr = families.getFamilies().find(42);
	const auto& familyItr2 = families.getFamilies().find(43);
	const auto& familyItr3 = families.getFamilies().find(44);

	ASSERT_EQ(families.getFamilies().end(), familyItr);
	ASSERT_EQ(43, familyItr2->first);
	ASSERT_EQ(43, familyItr2->second->getID());
	ASSERT_EQ(families.getFamilies().end(), familyItr3);
	ASSERT_EQ(1, families.getFamilies().size());
}