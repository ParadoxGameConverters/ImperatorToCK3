#include "Mappers/RegionMapper/ImperatorRegion.h"
#include "gtest/gtest.h"
#include <sstream>


TEST(Mappers_ImperatorRegionTests, blankRegionLoadsWithNoAreas)
{
	std::stringstream input;
	const mappers::ImperatorRegion region(input);

	ASSERT_TRUE(region.getAreas().empty());
}

TEST(Mappers_ImperatorRegionTests, areaCanBeLoaded)
{
	std::stringstream input;
	input << "areas = { testarea } \n";
	const mappers::ImperatorRegion region(input);

	ASSERT_FALSE(region.getAreas().empty());
	ASSERT_EQ("testarea", region.getAreas().find("testarea")->first);
}

TEST(Mappers_ImperatorRegionTests, multipleAreasCanBeLoaded)
{
	std::stringstream input;
	input << "areas = { test1 test2 test3 } \n";
	const mappers::ImperatorRegion region(input);

	ASSERT_EQ(3, region.getAreas().size());
}

TEST(Mappers_ImperatorRegionTests, regionCanBeLinkedToArea)
{
	std::stringstream input;
	input << "areas = { test1 test2 test3 } \n";
	mappers::ImperatorRegion region(input);

	std::stringstream input2;
	input2 << "{ provinces  = { 3 6 2 }} \n";
	auto area = std::make_shared<mappers::ImperatorArea>(input2);

	ASSERT_EQ(nullptr, region.getAreas().find("test2")->second); // nullptr before linking
	region.linkArea("test2", area);
	ASSERT_TRUE(region.getAreas().find("test2")->second);
}

TEST(Mappers_ImperatorRegionTests, linkedRegionCanLocateProvince)
{
	std::stringstream input;
	input << "duchies = { d_ivrea d_athens d_oppo } \n";
	mappers::ImperatorRegion region(input);

	std::stringstream input2;
	input2 << "{ provinces  = { 3 6 2 }} \n";
	auto area = std::make_shared<mappers::ImperatorArea>(input2);

	region.linkArea("test2", area);

	ASSERT_TRUE(region.regionContainsProvince(6));
}

TEST(Mappers_ImperatorRegionTests, linkedRegionWillFailForProvinceMismatch)
{
	std::stringstream input;
	input << "duchies = { d_ivrea d_athens d_oppo } \n";
	mappers::ImperatorRegion region(input);

	std::stringstream input2;
	input2 << "{ provinces  = { 3 6 2 }} \n";
	auto area = std::make_shared<mappers::ImperatorArea>(input2);

	region.linkArea("test2", area);

	ASSERT_FALSE(region.regionContainsProvince(7));
}
