#include "Mappers/RegionMapper/ImperatorArea.h"
#include "gtest/gtest.h"
#include <sstream>


TEST(Mappers_ImperatorAreaTests, blankAreaLoadsWithNoProvinces)
{
	std::stringstream input;
	const mappers::ImperatorArea area(input);

	ASSERT_TRUE(area.getProvinces().empty());
}

TEST(Mappers_ImperatorAreaTests, provinceCanBeLoaded)
{
	std::stringstream input;
	input << "provinces = { 69 } \n";
	const mappers::ImperatorArea area(input);

	ASSERT_FALSE(area.getProvinces().empty());
	ASSERT_EQ(69, *area.getProvinces().find(69));
}


TEST(Mappers_ImperatorAreaTests, multipleProvincesCanBeLoaded)
{
	std::stringstream input;
	input << "provinces = { 2 69 3 } \n";
	const mappers::ImperatorArea area(input);

	ASSERT_EQ(3, area.getProvinces().size());
}
