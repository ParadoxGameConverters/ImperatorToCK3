#include "gtest/gtest.h"
#include <sstream>

#include "../ImperatorToCK3/Source/Imperator/Provinces/Provinces.h"
#include "../ImperatorToCK3/Source/Imperator/Provinces/Province.h"

TEST(ImperatorWorld_ProvincesTests, provincesDefaultToEmpty)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "}";

	const ImperatorWorld::Provinces provinces(input);

	ASSERT_TRUE(provinces.getProvinces().empty());
}

TEST(ImperatorWorld_ProvincesTests, provincesCanBeLoaded)
{
	std::stringstream input;
	input << "=\n";
	input << "{\n";
	input << "42={}\n";
	input << "43={}\n";
	input << "}";

	const ImperatorWorld::Provinces provinces(input);
	const auto& provinceItr = provinces.getProvinces().find(42);
	const auto& provinceItr2 = provinces.getProvinces().find(43);

	ASSERT_EQ(42, provinceItr->first);
	ASSERT_EQ(42, provinceItr->second->getID());
	ASSERT_EQ(43, provinceItr2->first);
	ASSERT_EQ(43, provinceItr2->second->getID());
}