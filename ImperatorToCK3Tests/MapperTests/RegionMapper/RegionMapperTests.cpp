#include "../../ImperatorToCK3/Source/Mappers/RegionMapper/RegionMapper.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_RegionMapperTests, regionMapperCanBeEnabled)
{
	// We start humble, it's a machine.
	mappers::RegionMapper theMapper;
	CK3::LandedTitles landedTitles;
	std::stringstream regionStream;
	std::stringstream superRegionStream;

	theMapper.loadRegions(landedTitles, regionStream, superRegionStream);
	ASSERT_FALSE(theMapper.provinceIsInRegion(1, "test"));
	ASSERT_FALSE(theMapper.regionNameIsValid("test"));
	ASSERT_FALSE(theMapper.getParentCountyName(1));
	ASSERT_FALSE(theMapper.getParentDuchyName(1));
	ASSERT_FALSE(theMapper.getParentRegionName(1));
}
TEST(Mappers_RegionMapperTests, loadingBrokenRegionWillThrowException)
{
	mappers::RegionMapper theMapper;
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_anglia = { d_aquitane = { c_mers = { b_hgy = { province = 69 } } } } \n";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region_broken = { counties = { c_mers } }";
	regionStream << "test_region2 = { regions = { test_region } }";
	std::stringstream islandRegionStream;
	try
	{
		theMapper.loadRegions(landedTitles, regionStream, islandRegionStream);
		FAIL();
	}
	catch (const std::runtime_error& e)
	{
		ASSERT_STREQ("Region's test_region2 region test_region does not exist!", e.what());
	}
}
TEST(Mappers_RegionMapperTests, loadingBrokenDuchyWillThrowException)
{
	mappers::RegionMapper theMapper;
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_anglia = { d_broken_aquitane = { c_mers = { b_hgy = { province = 69 } } } } \n";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_aquitane } }";
	std::stringstream islandRegionStream;
	try
	{
		theMapper.loadRegions(landedTitles, regionStream, islandRegionStream);
		FAIL();
	}
	catch (const std::runtime_error& e)
	{
		ASSERT_STREQ("Region's test_region duchy d_aquitane does not exist!", e.what());
	}
}
TEST(Mappers_RegionMapperTests, loadingBrokenCountyWillThrowException)
{
	mappers::RegionMapper theMapper;
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_anglia = { d_aquitane = { c_mers_broken = { b_hgy = { province = 69 } } } } \n";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region = { counties = { c_mers } }";
	std::stringstream islandRegionStream;
	try
	{
		theMapper.loadRegions(landedTitles, regionStream, islandRegionStream);
		FAIL();
	}
	catch (const std::runtime_error& e)
	{
		ASSERT_STREQ("Region's test_region county c_mers does not exist!", e.what());
	}
}

TEST(Mappers_RegionMapperTests, locationServicesWork)
{
	mappers::RegionMapper theMapper;
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "d_aquitane = { c_mers = { b_hgy = { province = 69 } } }";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_aquitane } }\n";
	regionStream << "test_region_bigger = { regions = { test_region } }\n";
	regionStream << "test_region_biggest = { regions = { test_region_bigger } }\n";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitles, regionStream, islandRegionStream);

	ASSERT_TRUE(theMapper.provinceIsInRegion(69, "c_mers"));
	ASSERT_TRUE(theMapper.provinceIsInRegion(69, "d_aquitane"));
	ASSERT_TRUE(theMapper.provinceIsInRegion(69, "test_region"));
	ASSERT_TRUE(theMapper.provinceIsInRegion(69, "test_region_bigger"));
	ASSERT_TRUE(theMapper.provinceIsInRegion(69, "test_region_biggest"));
}

TEST(Mappers_RegionMapperTests, locationServicesCorrectlyFail)
{
	mappers::RegionMapper theMapper;
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "d_testduchy = { 1 2 3 } \n";
	landedTitlesStream << "d_testduchy2 = { 4 5 6 } ";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_testduchy } }";
	regionStream << "test_region2 = { duchies = { d_testduchy2 } }\n";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitles, regionStream, islandRegionStream);

	ASSERT_FALSE(theMapper.provinceIsInRegion(4, "d_testduchy"));
	ASSERT_FALSE(theMapper.provinceIsInRegion(5, "test_region"));
	ASSERT_FALSE(theMapper.provinceIsInRegion(6, "test_superregion"));
}

TEST(Mappers_RegionMapperTests, locationServicesFailForNonsense)
{
	mappers::RegionMapper theMapper;
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "d_testduchy = { 1 2 3 } \n";
	landedTitlesStream << "d_testduchy2 = { 4 5 6 } ";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_testduchy } }";
	regionStream << "test_region2 = { duchies = { d_testduchy2 } }\n";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitles, regionStream, islandRegionStream);

	ASSERT_FALSE(theMapper.provinceIsInRegion(1, "nonsense"));
	ASSERT_FALSE(theMapper.provinceIsInRegion(9, "d_testduchy"));
}

TEST(Mappers_RegionMapperTests, correctParentLocationsReported)
{
	mappers::RegionMapper theMapper;
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } } \n";
	landedTitlesStream << "k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_wakaba } }";
	regionStream << "test_region2 = { duchies = { d_hujhu } }\n";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitles, regionStream, islandRegionStream);

	ASSERT_EQ("c_athens", *theMapper.getParentCountyName(79));
	ASSERT_EQ("d_wakaba", *theMapper.getParentDuchyName(79));
	ASSERT_EQ("test_region", *theMapper.getParentRegionName(79));
	ASSERT_EQ("c_defff", *theMapper.getParentCountyName(6));
	ASSERT_EQ("d_hujhu", *theMapper.getParentDuchyName(6));
	ASSERT_EQ("test_region2", *theMapper.getParentRegionName(6));
}

TEST(Mappers_RegionMapperTests, wrongParentLocationsReturnEmpty)
{
	mappers::RegionMapper theMapper;
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "d_testduchy = { 1 2 3 } \n";
	landedTitlesStream << "d_testduchy2 = { 4 5 6 } ";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_testduchy } }";
	regionStream << "test_region2 = { duchies = { d_testduchy2 } }\n";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitles, regionStream, islandRegionStream);

	ASSERT_FALSE(theMapper.getParentCountyName(7));
	ASSERT_FALSE(theMapper.getParentDuchyName(7));
	ASSERT_FALSE(theMapper.getParentRegionName(7));
}

TEST(Mappers_RegionMapperTests, locationNameValidationWorks)
{
	mappers::RegionMapper theMapper;
	
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } } \n";
	landedTitlesStream << "k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n";
	landedTitlesStream << "c_county = { b_barony = { province = 69 } } \n";
	landedTitles.loadTitles(landedTitlesStream);
	
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_wakaba } }";
	regionStream << "test_region2 = { duchies = { d_hujhu } }\n";
	regionStream << "test_region3 = { regions = { test_region test_region2 } counties = { c_county } }\n";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitles, regionStream, islandRegionStream);

	ASSERT_TRUE(theMapper.regionNameIsValid("d_wakaba"));
	ASSERT_TRUE(theMapper.regionNameIsValid("test_region2"));
	ASSERT_TRUE(theMapper.regionNameIsValid("test_region3"));
	ASSERT_TRUE(theMapper.regionNameIsValid("c_county"));
	ASSERT_FALSE(theMapper.regionNameIsValid("nonsense"));
}


TEST(Mappers_RegionMapperTests, locationServicesSucceedsForProvinceField)
{
	mappers::RegionMapper theMapper;
	CK3::LandedTitles landedTitles;
	std::stringstream regionStream;
	regionStream << "test_region = { provinces = { 1 2 69 7 } }";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitles, regionStream, islandRegionStream);

	ASSERT_TRUE(theMapper.provinceIsInRegion(69, "test_region"));
}

TEST(Mappers_RegionMapperTests, locationServicesSucceedsForCountyField)
{
	mappers::RegionMapper theMapper;
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } }";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region = { counties = { c_athens } }";
	std::stringstream islandRegionStream;
	theMapper.loadRegions(landedTitles, regionStream, islandRegionStream);

	ASSERT_TRUE(theMapper.provinceIsInRegion(79, "test_region"));
}