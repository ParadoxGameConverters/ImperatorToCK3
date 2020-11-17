#include "Mappers/RegionMapper/ImperatorRegionMapper.h"
#include "gtest/gtest.h"
#include <sstream>


TEST(Mappers_ImperatorRegionMapperTests, regionMapperCanBeEnabled)
{
	// We start humble, it's a machine.
	mappers::ImperatorRegionMapper theMapper;
	std::stringstream areaStream;
	std::stringstream regionStream;

	theMapper.loadRegions(areaStream, regionStream);
	ASSERT_FALSE(theMapper.provinceIsInRegion(1, "test"));
	ASSERT_FALSE(theMapper.regionNameIsValid("test"));
	ASSERT_FALSE(theMapper.getParentAreaName(1));
	ASSERT_FALSE(theMapper.getParentRegionName(1));
}
TEST(Mappers_ImperatorRegionMapperTests, loadingBrokenAreaWillThrowException)
{
	mappers::ImperatorRegionMapper theMapper;
	std::stringstream areaStream;
	std::stringstream regionStream;
	regionStream << "test_region = { areas = { testarea } }";
	try
	{
		theMapper.loadRegions(areaStream, regionStream);
		FAIL();
	}
	catch (const std::runtime_error& e)
	{
		ASSERT_STREQ("Region's test_region area testarea does not exist!", e.what());
	}
}


TEST(Mappers_ImperatorRegionMapperTests, locationServicesWork)
{
	mappers::ImperatorRegionMapper theMapper;
	std::stringstream areaStream;
	areaStream << "test_area = { provinces = { 1 2 3 } }\n";
	std::stringstream regionStream;
	regionStream << "test_region = { areas = { test_area } }\n";
	theMapper.loadRegions(areaStream, regionStream);

	ASSERT_TRUE(theMapper.provinceIsInRegion(3, "test_area"));
	ASSERT_TRUE(theMapper.provinceIsInRegion(3, "test_region"));
}

TEST(Mappers_ImperatorRegionMapperTests, locationServicesCorrectlyFail)
{
	mappers::ImperatorRegionMapper theMapper;
	std::stringstream areaStream;
	areaStream << "test_area = { provinces = { 1 2 3 } }\n";
	areaStream << "test_area2 = { provinces = { 4 5 6 } }\n";
	areaStream << "test_area3 = { provinces = { 7 8 9 } }\n";
	std::stringstream regionStream;
	regionStream << "test_region = { areas = { test_area test_area2 } }\n";
	regionStream << "test_region2 = { areas = { test_area3 } }\n";
	theMapper.loadRegions(areaStream, regionStream);

	ASSERT_FALSE(theMapper.provinceIsInRegion(3, "test_area2")); // province in different area
	ASSERT_FALSE(theMapper.provinceIsInRegion(9, "test_region")); // province in different region
	ASSERT_FALSE(theMapper.provinceIsInRegion(9, "test_region")); // province missing completely
}

TEST(Mappers_ImperatorRegionMapperTests, locationServicesFailForNonsense)
{
	mappers::ImperatorRegionMapper theMapper;
	std::stringstream areaStream;
	areaStream << "test1 = { provinces = { 1 2 3 } }";
	std::stringstream regionStream;
	regionStream << "test_region = { areas = { test1 } }";
	theMapper.loadRegions(areaStream, regionStream);

	ASSERT_FALSE(theMapper.provinceIsInRegion(1, "nonsense"));
}

TEST(Mappers_ImperatorRegionMapperTests, correctParentLocationsReported)
{
	mappers::ImperatorRegionMapper theMapper;
	std::stringstream areaStream;
	areaStream << "test_area = { provinces = { 1 2 3 } }\n";
	areaStream << "test_area2 = { provinces = { 4 5 6 } }\n";
	std::stringstream regionStream;
	regionStream << "test_region = { areas = { test_area } }\n";
	regionStream << "test_region2 = { areas = { test_area2 } }\n";
	theMapper.loadRegions(areaStream, regionStream);

	ASSERT_EQ("test_area", *theMapper.getParentAreaName(2));
	ASSERT_EQ("test_region", *theMapper.getParentRegionName(2));
	ASSERT_EQ("test_area2", *theMapper.getParentAreaName(5));
	ASSERT_EQ("test_region2", *theMapper.getParentRegionName(5));
}

TEST(Mappers_ImperatorRegionMapperTests, wrongParentLocationsReturnNullopt)
{
	mappers::ImperatorRegionMapper theMapper;

	std::stringstream areaStream;
	areaStream << "test_area = { provinces = { 1 2 3 } }\n";
	std::stringstream regionStream;
	regionStream << "test_region = { areas = { test_area } }\n";
	theMapper.loadRegions(areaStream, regionStream);

	ASSERT_FALSE(theMapper.getParentAreaName(5));
	ASSERT_FALSE(theMapper.getParentRegionName(5));
}

TEST(Mappers_ImperatorRegionMapperTests, locationNameValidationWorks)
{
	mappers::ImperatorRegionMapper theMapper;
	
	std::stringstream areaStream;
	areaStream << "test_area = { provinces = { 1 2 3 } }";
	areaStream << "test_area2 = { provinces = { 4 5 6 } }\n";
	std::stringstream regionStream;
	regionStream << "test_region = { areas = { test_area } }";
	regionStream << "test_region2 = { areas = { test_area2 } }\n";
	theMapper.loadRegions(areaStream, regionStream);

	ASSERT_TRUE(theMapper.regionNameIsValid("test_area"));
	ASSERT_TRUE(theMapper.regionNameIsValid("test_area2"));
	ASSERT_TRUE(theMapper.regionNameIsValid("test_region"));
	ASSERT_TRUE(theMapper.regionNameIsValid("test_region2"));
	ASSERT_FALSE(theMapper.regionNameIsValid("nonsense"));
}
