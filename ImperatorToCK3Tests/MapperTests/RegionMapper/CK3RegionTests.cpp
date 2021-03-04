#include "Mappers/RegionMapper/CK3Region.h"
#include "CK3/Titles/Title.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_RegionTests, blankRegionLoadsWithNoRegionsAndNoDuchies)
{
	std::stringstream input;
	const mappers::CK3Region region(input);

	ASSERT_TRUE(region.getRegions().empty());
	ASSERT_TRUE(region.getDuchies().empty());
}

TEST(Mappers_RegionTests, areaCanBeLoaded)
{
	std::stringstream input;
	input << "duchies = { d_ivrea } \n";
	const mappers::CK3Region region(input);

	ASSERT_FALSE(region.getDuchies().empty());
	ASSERT_EQ("d_ivrea", region.getDuchies().find("d_ivrea")->first);
}

TEST(Mappers_RegionTests, regionCanBeLoaded)
{
	std::stringstream input;
	input << "regions = { sicily_region } \n";
	const mappers::CK3Region region(input);

	ASSERT_FALSE(region.getRegions().empty());
	ASSERT_EQ("sicily_region", region.getRegions().find("sicily_region")->first);
}

TEST(Mappers_RegionTests, multipleDuchiesCanBeLoaded)
{
	std::stringstream input;
	input << "duchies = { d_ivrea d_athens d_oppo } \n";
	const mappers::CK3Region region(input);

	ASSERT_EQ(3, region.getDuchies().size());
}

TEST(Mappers_RegionTests, multipleRegionsCanBeLoaded)
{
	std::stringstream input;
	input << "regions = { sicily_region island_region new_region } \n";
	const mappers::CK3Region region(input);

	ASSERT_EQ(3, region.getRegions().size());
}

TEST(Mappers_RegionTests, regionCanBeLinkedToDuchy)
{
	std::stringstream input;
	input << "duchies = { d_ivrea d_athens d_oppo } \n";
	mappers::CK3Region region(input);

	std::stringstream input2;
	input2 << "{ c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } \n";
	auto duchy2 = std::make_shared<CK3::Title>("d_athens");
	duchy2->loadTitles(input2);

	ASSERT_EQ(nullptr, region.getDuchies().find("d_athens")->second); // nullptr before linking
	region.linkDuchy(duchy2);
	ASSERT_TRUE(region.getDuchies().find("d_athens")->second);
}

TEST(Mappers_RegionTests, LinkedRegionCanLocateProvince)
{
	std::stringstream input;
	input << "duchies = { d_ivrea d_athens d_oppo } \n";
	mappers::CK3Region region(input);

	std::stringstream input2;
	input2 << "= { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } \n";
	auto duchy2 = std::make_shared<CK3::Title>("d_athens");
	duchy2->loadTitles(input2);
	
	region.linkDuchy(duchy2);

	ASSERT_TRUE(region.regionContainsProvince(79));
	ASSERT_TRUE(region.regionContainsProvince(56));
}

TEST(Mappers_RegionTests, LinkedRegionWillFailForProvinceMismatch)
{
	std::stringstream input;
	input << "duchies = { d_ivrea d_athens d_oppo } \n";
	mappers::CK3Region region(input);

	std::stringstream input2;
	input2 << "{ c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } \n";
	auto duchy2 = std::make_shared<CK3::Title>("d_athens");
	duchy2->loadTitles(input2);

	region.linkDuchy(duchy2);

	ASSERT_FALSE(region.regionContainsProvince(7));
}
