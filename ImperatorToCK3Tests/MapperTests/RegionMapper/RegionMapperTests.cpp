#include "../../ImperatorToCK3/Source/Mappers/RegionMapper/RegionMapper.h"
#include "gtest/gtest.h"
#include <sstream>

// This is a collective mapper test for Area, Region, SuperRegion and RegionMapper
// submodules. They can't work without each other in concert.

TEST(Mappers_RegionMapperTests, regionMapperCanBeEnabled)
{
	// We start humble, it's a machine.
	mappers::RegionMapper theMapper;
	std::stringstream landedTitlesStream;
	std::stringstream regionStream;
	std::stringstream superRegionStream;

	theMapper.loadRegions(landedTitlesStream, regionStream, superRegionStream);
	ASSERT_FALSE(theMapper.provinceIsInRegion(1, "test"));
	ASSERT_FALSE(theMapper.regionNameIsValid("test"));
	ASSERT_FALSE(theMapper.getParentCountyName(1));
	ASSERT_FALSE(theMapper.getParentDuchyName(1));
	ASSERT_FALSE(theMapper.getParentRegionName(1));
}

TEST(Mappers_RegionMapperTests, loadingBrokenAreaWillThrowException)
{
	mappers::RegionMapper theMapper;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_anglia = { d_broken_aquitane = { c_mers = { b_hgy = { province = 69 } } } } \n";
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_aquitane } }";
	std::stringstream islandRegionStream;
	try
	{
		theMapper.loadRegions(landedTitlesStream, regionStream, islandRegionStream);
		FAIL();
	}
	catch (const std::runtime_error& e)
	{
		ASSERT_STREQ("Region's test_region duchy d_aquitane does not exist!", e.what());
	}
}

TEST(Mappers_RegionMapperTests, locationServicesWork)
{
	mappers::RegionMapper theMapper;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "d_aquitane = { c_mers = { b_hgy = { province = 69 } } }";
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_aquitane } }\n";
	regionStream << "test_region_bigger = { regions = { test_region } }\n";
	regionStream << "test_region_biggest = { regions = { test_region_bigger } }\n";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitlesStream, regionStream, islandRegionStream);

	ASSERT_TRUE(theMapper.provinceIsInRegion(69, "test_region"));
	ASSERT_TRUE(theMapper.provinceIsInRegion(69, "test_region_bigger"));
	ASSERT_TRUE(theMapper.provinceIsInRegion(69, "test_region_biggest"));
}

TEST(Mappers_RegionMapperTests, locationServicesCorrectlyFail)
{
	mappers::RegionMapper theMapper;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "test_area = { 1 2 3 } \n";
	landedTitlesStream << "test_area2 = { 4 5 6 } ";
	std::stringstream regionStream;
	regionStream << "test_region = { areas = { test_area } }";
	regionStream << "test_region2 = { areas = { test_area2 } }\n";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitlesStream, regionStream, islandRegionStream);

	ASSERT_FALSE(theMapper.provinceIsInRegion(4, "test_area"));
	ASSERT_FALSE(theMapper.provinceIsInRegion(5, "test_region"));
	ASSERT_FALSE(theMapper.provinceIsInRegion(6, "test_superregion"));
}

TEST(Mappers_RegionMapperTests, locationServicesFailForNonsense)
{
	mappers::RegionMapper theMapper;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "test_area = { 1 2 3 } \n";
	landedTitlesStream << "test_area2 = { 4 5 6 } ";
	std::stringstream regionStream;
	regionStream << "test_region = { areas = { test_area } }";
	regionStream << "test_region2 = { areas = { test_area2 } }\n";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitlesStream, regionStream, islandRegionStream);

	ASSERT_FALSE(theMapper.provinceIsInRegion(1, "nonsense"));
	ASSERT_FALSE(theMapper.provinceIsInRegion(9, "test_area"));
}

TEST(Mappers_RegionMapperTests, correctParentLocationsReported)
{
	mappers::RegionMapper theMapper;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } } \n";
	landedTitlesStream << "k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n";
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_wakaba } }";
	regionStream << "test_region2 = { duchies = { d_hujhu } }\n";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitlesStream, regionStream, islandRegionStream);

	ASSERT_EQ(*theMapper.getParentCountyName(1), "c_athens");
	ASSERT_EQ(*theMapper.getParentDuchyName(1), "d_wakaba");
	ASSERT_EQ(*theMapper.getParentRegionName(1), "test_region");
	ASSERT_EQ(*theMapper.getParentCountyName(1), "c_defff");
	ASSERT_EQ(*theMapper.getParentDuchyName(1), "d_hujhu");
	ASSERT_EQ(*theMapper.getParentRegionName(1), "test_region2");
}

TEST(Mappers_RegionMapperTests, wrongParentLocationsReturnEmpty)
{
	mappers::RegionMapper theMapper;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "test_area = { 1 2 3 } \n";
	landedTitlesStream << "test_area2 = { 4 5 6 } ";
	std::stringstream regionStream;
	regionStream << "test_region = { areas = { test_area } }";
	regionStream << "test_region2 = { areas = { test_area2 } }\n";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitlesStream, regionStream, islandRegionStream);

	ASSERT_FALSE(theMapper.getParentCountyName(7));
	ASSERT_FALSE(theMapper.getParentDuchyName(7));
	ASSERT_FALSE(theMapper.getParentRegionName(7));
}

TEST(Mappers_RegionMapperTests, locationNameValidationWorks)
{
	mappers::RegionMapper theMapper;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } } \n";
	landedTitlesStream << "k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n";
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_wakaba } }";
	regionStream << "test_region2 = { duchies = { d_hujhu } }\n";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitlesStream, regionStream, islandRegionStream);

	ASSERT_TRUE(theMapper.regionNameIsValid("test_area"));
	ASSERT_TRUE(theMapper.regionNameIsValid("test_region2"));
	ASSERT_FALSE(theMapper.regionNameIsValid("nonsense"));
}
