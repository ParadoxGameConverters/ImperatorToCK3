#include "Mappers/ReligionMapper/ReligionMapping.h"
#include "Mappers/RegionMapper/CK3RegionMapper.h"
#include "Mappers/RegionMapper/ImperatorRegionMapper.h"
#include "CK3/Titles/LandedTitles.h"
#include "gtest/gtest.h"
#include <sstream>



TEST(Mappers_ReligionMappingTests, regularMatchOnSimpleReligion)
{
	std::stringstream input;
	input << "ck3 = flemish imp = dutch";
	const mappers::ReligionMapping theMapping(input);

	ASSERT_EQ("flemish", *theMapping.match("dutch", 0, 0));
}

TEST(Mappers_ReligionMappingTests, matchOnProvince)
{
	std::stringstream input;
	input << "ck3 = dutch imp = german ck3Province = 17";
	const mappers::ReligionMapping theMapping(input);

	ASSERT_EQ("dutch", *theMapping.match("german", 17, 0));
}

TEST(Mappers_ReligionMappingTests, matchOnProvinceFailsForWrongProvince)
{
	std::stringstream input;
	input << "ck3 = dutch imp = german ck3Province = 17";
	const mappers::ReligionMapping theMapping(input);

	ASSERT_FALSE(theMapping.match("german", 19, 0));
}

TEST(Mappers_ReligionMappingTests, matchOnProvinceFailsForNoProvince)
{
	std::stringstream input;
	input << "ck3 = dutch imp = german ck3Province = 17";
	const mappers::ReligionMapping theMapping(input);

	ASSERT_FALSE(theMapping.match("german", 0, 0));
}

TEST(Mappers_ReligionMappingTests, matchOnRegion)
{
	auto theMapper = std::make_shared<mappers::CK3RegionMapper>();
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_ghef = { d_hujhu = { c_defff = { b_newbarony2 = { province = 4 } } } } \n";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region1 = { duchies = { d_hujhu } }\n";
	std::stringstream islandRegionStream;
	theMapper->loadRegions(landedTitles, regionStream, islandRegionStream);

	std::stringstream input;
	input << "ck3 = dutch imp = german ck3Region = test_region1";
	mappers::ReligionMapping theMapping(input);
	theMapping.insertCK3RegionMapper(theMapper);
	theMapping.insertImperatorRegionMapper(std::make_shared<mappers::ImperatorRegionMapper>());

	ASSERT_EQ("dutch", *theMapping.match("german", 4, 0));
}

TEST(Mappers_ReligionMappingTests, matchOnRegionFailsForWrongRegion)
{
	auto theMapper = std::make_shared<mappers::CK3RegionMapper>();
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } } } } \n";
	landedTitlesStream << "k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } } } } \n";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_wakaba } }";
	regionStream << "test_region2 = { duchies = { d_hujhu } }\n";
	std::stringstream islandRegionStream;
	theMapper->loadRegions(landedTitles, regionStream, islandRegionStream);

	std::stringstream input;
	input << "ck3 = dutch imp = german ck3Region = test_region2";
	mappers::ReligionMapping theMapping(input);
	theMapping.insertCK3RegionMapper(theMapper);
	theMapping.insertImperatorRegionMapper(std::make_shared<mappers::ImperatorRegionMapper>());

	ASSERT_FALSE(theMapping.match("german", 79, 0));
}

TEST(Mappers_ReligionMappingTests, matchOnRegionFailsForNoRegion)
{
	auto ck3Mapper = std::make_shared<mappers::CK3RegionMapper>();
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	std::stringstream islandRegionStream;
	ck3Mapper->loadRegions(landedTitles, regionStream, islandRegionStream);

	std::stringstream input;
	input << "ck3 = dutch imp = german ck3Region = test_region3";
	mappers::ReligionMapping theMapping(input);
	theMapping.insertCK3RegionMapper(ck3Mapper);
	theMapping.insertImperatorRegionMapper(std::make_shared<mappers::ImperatorRegionMapper>());

	ASSERT_FALSE(theMapping.match("german", 17, 0));
}

TEST(Mappers_ReligionMappingTests, matchOnRegionFailsForNoProvince)
{
	auto theMapper = std::make_shared<mappers::CK3RegionMapper>();
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	std::stringstream islandRegionStream;
	theMapper->loadRegions(landedTitles, regionStream, islandRegionStream);

	std::stringstream input;
	input << "ck3 = dutch imp = german ck3Region = d_hujhu";
	mappers::ReligionMapping theMapping(input);
	theMapping.insertCK3RegionMapper(theMapper);
	theMapping.insertImperatorRegionMapper(std::make_shared<mappers::ImperatorRegionMapper>());

	ASSERT_FALSE(theMapping.match("german", 0, 0));
}
