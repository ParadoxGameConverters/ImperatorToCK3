#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/Imperator/Countries/Country.h"
#include <sstream>

TEST(ImperatorWorld_CountryTests, IDCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_EQ(42, theCountry.getID());
}

TEST(ImperatorWorld_CountryTests, tagCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\ttag=\"WTF\"";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_EQ("WTF", theCountry.getTag());
}

TEST(ImperatorWorld_CountryTests, tagDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_TRUE(theCountry.getTag().empty());
}

TEST(ImperatorWorld_CountryTests, nameCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "country_name = {\n";
	input << "name=\"WTF\"\n";
	input << "}\n";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_EQ("WTF", theCountry.getName());
}

TEST(ImperatorWorld_CountryTests, nameDefaultsToBlank)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_TRUE(theCountry.getName().empty());
}

TEST(ImperatorWorld_CountryTests, capitalCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tcapital = 32\n";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_EQ(32, theCountry.getCapital());
}

TEST(ImperatorWorld_CountryTests, capitalDefaultsToZero)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_EQ(0, theCountry.getCapital());
}

TEST(ImperatorWorld_CountryTests, currenciesDefaultToProperValues)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_EQ(0, theCountry.getCurrencies().manpower);
	ASSERT_EQ(0, theCountry.getCurrencies().gold);
	ASSERT_EQ(50, theCountry.getCurrencies().stability);
	ASSERT_EQ(0, theCountry.getCurrencies().tyranny);
	ASSERT_EQ(0, theCountry.getCurrencies().war_exhaustion);
	ASSERT_EQ(0, theCountry.getCurrencies().aggressive_expansion);
	ASSERT_EQ(0, theCountry.getCurrencies().political_influence);
	ASSERT_EQ(0, theCountry.getCurrencies().military_experience);
}

TEST(ImperatorWorld_CountryTests, currenciesCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tcurrency_data={ manpower=1 gold=2 stability=69 tyranny=4 war_exhaustion=2 aggressive_expansion=50 political_influence=4 military_experience=1}";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_EQ(1, theCountry.getCurrencies().manpower);
	ASSERT_EQ(2, theCountry.getCurrencies().gold);
	ASSERT_EQ(69, theCountry.getCurrencies().stability);
	ASSERT_EQ(4, theCountry.getCurrencies().tyranny);
	ASSERT_EQ(2, theCountry.getCurrencies().war_exhaustion);
	ASSERT_EQ(50, theCountry.getCurrencies().aggressive_expansion);
	ASSERT_EQ(4, theCountry.getCurrencies().political_influence);
	ASSERT_EQ(1, theCountry.getCurrencies().military_experience);
}