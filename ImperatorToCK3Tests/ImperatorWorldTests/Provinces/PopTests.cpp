#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/Imperator/Provinces/Pop.h"
#include "../ImperatorToCK3/Source/Imperator/Provinces/PopFactory.h"
#include <sstream>


Imperator::Pop::Factory popFactory;
TEST(ImperatorWorld_PopTests, IDCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto thePop = *popFactory.getPop("42", input);

	ASSERT_EQ(42, thePop.ID);
}
TEST(ImperatorWorld_PopTests, cultureCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tculture=\"paradoxian\"";
	input << "}";

	const auto thePop = *popFactory.getPop("42", input);

	ASSERT_EQ("paradoxian", thePop.culture);
}

TEST(ImperatorWorld_PopTests, cultureDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto thePop = *popFactory.getPop("42", input);

	ASSERT_TRUE(thePop.culture.empty());
}


TEST(ImperatorWorld_PopTests, religionCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\treligion=\"paradoxian\"";
	input << "}";

	const auto thePop = *popFactory.getPop("42", input);

	ASSERT_EQ("paradoxian", thePop.religion);
}

TEST(ImperatorWorld_PopTests, religionDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto thePop = *popFactory.getPop("42", input);

	ASSERT_TRUE(thePop.religion.empty());
}

TEST(ImperatorWorld_PopTests,typeCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\ttype = \"citizen\"\n";
	input << "}";

	const auto thePop = *popFactory.getPop("42", input);

	ASSERT_EQ("citizen", thePop.type);
}

TEST(ImperatorWorld_PopTests, typeDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const auto thePop = *popFactory.getPop("42", input);

	ASSERT_TRUE(thePop.type.empty());
}