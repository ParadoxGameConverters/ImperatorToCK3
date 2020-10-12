#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/Imperator/Provinces/Pop.h"
#include <sstream>

TEST(ImperatorWorld_PopTests, IDCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const Imperator::Pop thePop(input, 42);

	ASSERT_EQ(42, thePop.getID());
}
TEST(ImperatorWorld_PopTests, cultureCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tculture=\"paradoxian\"";
	input << "}";

	const Imperator::Pop thePop(input, 42);

	ASSERT_EQ("paradoxian", thePop.getCulture());
}

TEST(ImperatorWorld_PopTests, cultureDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const Imperator::Pop thePop(input, 42);

	ASSERT_TRUE(thePop.getCulture().empty());
}


TEST(ImperatorWorld_PopTests, religionCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\treligion=\"paradoxian\"";
	input << "}";

	const Imperator::Pop thePop(input, 42);

	ASSERT_EQ("paradoxian", thePop.getReligion());
}

TEST(ImperatorWorld_PopTests, religionDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const Imperator::Pop thePop(input, 42);

	ASSERT_TRUE(thePop.getReligion().empty());
}

TEST(ImperatorWorld_PopTests,typeCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\ttype = \"citizen\"\n";
	input << "}";

	const Imperator::Pop thePop(input, 42);

	ASSERT_EQ("citizen", thePop.getType());
}

TEST(ImperatorWorld_PopTests, typeDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const Imperator::Pop thePop(input, 42);

	ASSERT_TRUE(thePop.getType().empty());
}