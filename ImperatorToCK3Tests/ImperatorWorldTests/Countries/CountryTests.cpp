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

TEST(ImperatorWorld_CountryTests, capitalDefaultsTonNullopt)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_FALSE(theCountry.getCapital());
	ASSERT_EQ(std::nullopt, theCountry.getCapital());
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

TEST(ImperatorWorld_CountryTests, monarchCanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tmonarch=69";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_EQ(69, theCountry.getMonarch());
}

TEST(ImperatorWorld_CountryTests, monarchDefaultsToMinusOne)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_EQ(-1, theCountry.getMonarch());
}

TEST(ImperatorWorld_CountryTests, color1CanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tcolor = rgb { 69 4 20 }";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_EQ(69, theCountry.getColor1().value().r());
	ASSERT_EQ(4, theCountry.getColor1().value().g());
	ASSERT_EQ(20, theCountry.getColor1().value().b());
}

TEST(ImperatorWorld_CountryTests, color1DefaultsToNullopt)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_FALSE(theCountry.getColor1());
}

TEST(ImperatorWorld_CountryTests, color2CanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tcolor2 = rgb { 69 4 20 }";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_EQ(69, theCountry.getColor2().value().r());
	ASSERT_EQ(4, theCountry.getColor2().value().g());
	ASSERT_EQ(20, theCountry.getColor2().value().b());
}

TEST(ImperatorWorld_CountryTests, color2DefaultsToNullopt)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_FALSE(theCountry.getColor2());
}

TEST(ImperatorWorld_CountryTests, color3CanBeSet)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "\tcolor3 = rgb { 69 4 20 }";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_EQ(69, theCountry.getColor3().value().r());
	ASSERT_EQ(4, theCountry.getColor3().value().g());
	ASSERT_EQ(20, theCountry.getColor3().value().b());
}

TEST(ImperatorWorld_CountryTests, color3DefaultsToNullopt)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Country theCountry(input, 42);

	ASSERT_FALSE(theCountry.getColor3());
}