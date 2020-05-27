#include "gtest/gtest.h"
#include <sstream>

#include "../ImperatorToCK3/Source/Imperator/Provinces/Pops.h"
#include "../ImperatorToCK3/Source/Imperator/Provinces/Pop.h"

TEST(ImperatorWorld_PopsTests, popsDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	ImperatorWorld::Pops pops;
	pops.loadPops(input);

	ASSERT_TRUE(pops.getPops().empty());
}

TEST(ImperatorWorld_PopsTests, popsCanBeLoaded)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "42={}\n";
	input << "43={}\n";
	input << "}";

	ImperatorWorld::Pops pops;
	pops.loadPops(input);
	const auto& popItr = pops.getPops().find(42);
	const auto& popItr2 = pops.getPops().find(43);

	ASSERT_EQ(42, popItr->first);
	ASSERT_EQ(42, popItr->second->getID());
	ASSERT_EQ(43, popItr2->first);
	ASSERT_EQ(43, popItr2->second->getID());
}