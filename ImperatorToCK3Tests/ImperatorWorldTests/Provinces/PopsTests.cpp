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

	Imperator::Pops pops;
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

	Imperator::Pops pops;
	pops.loadPops(input);
	const auto& popItr = pops.getPops().find(42);
	const auto& popItr2 = pops.getPops().find(43);

	ASSERT_EQ(42, popItr->first);
	ASSERT_EQ(42, popItr->second->ID);
	ASSERT_EQ(43, popItr2->first);
	ASSERT_EQ(43, popItr2->second->ID);
}

TEST(ImperatorWorld_PopsTests, literalNonePopsAreNotLoaded)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "42=none\n";
	input << "43={}\n";
	input << "44=none\n";
	input << "}";

	Imperator::Pops pops;
	pops.loadPops(input);
	const auto& popItr = pops.getPops().find(42);
	const auto& popItr2 = pops.getPops().find(43);
	const auto& popItr3 = pops.getPops().find(44);

	ASSERT_EQ(pops.getPops().end(), popItr);
	ASSERT_EQ(43, popItr2->first);
	ASSERT_EQ(43, popItr2->second->ID);
	ASSERT_EQ(pops.getPops().end(), popItr3);
	ASSERT_EQ(1, pops.getPops().size());
}