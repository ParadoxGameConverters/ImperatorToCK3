#include "gtest/gtest.h"
#include <sstream>

#include "../ImperatorToCK3/Source/Imperator/Countries/Countries.h"
#include "../ImperatorToCK3/Source/Imperator/Countries/Country.h"

TEST(ImperatorWorld_CountriesTests, countriesDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Countries countries(input);

	ASSERT_TRUE(countries.getCountries().empty());
}

TEST(ImperatorWorld_CountriesTests, countriesCanBeLoaded)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "42={}\n";
	input << "43={}\n";
	input << "}";

	const ImperatorWorld::Countries countries(input);
	
	const auto& countryItr = countries.getCountries().find(42);
	const auto& countryItr2 = countries.getCountries().find(43);

	ASSERT_EQ(42, countryItr->first);
	ASSERT_EQ(42, countryItr->second->getID());
	ASSERT_EQ(43, countryItr2->first);
	ASSERT_EQ(43, countryItr2->second->getID());
	ASSERT_EQ(2, countries.getCountries().size());
}