#include "Mappers/ReligionMapper/ReligionMapping.h"
#include "Mappers/RegionMapper/CK3RegionMapper.h"
#include "Mappers/RegionMapper/ImperatorRegionMapper.h"
#include "gtest/gtest.h"
#include <sstream>



TEST(Mappers_ReligionMappingTests, regularMatchOnSimpleReligion)
{
	std::stringstream input;
	input << "ck3 = flemish imp = dutch";
	const mappers::ReligionMapping theMapping(input);

	ASSERT_EQ("flemish", *theMapping.religionMatch("dutch", 56, 445));
}

TEST(Mappers_ReligionMappingTests, regularMatchOnSimpleReligionFailsForWrongCulture)
{
	std::stringstream input;
	input << "ck3 = flemish imp = flemish";
	const mappers::ReligionMapping theMapping(input);

	ASSERT_FALSE(theMapping.religionMatch("dutch", 345, 435));
}


TEST(Mappers_ReligionMappingTests, matchOnProvince)
{
	std::stringstream input;
	input << "ck3 = dutch imp = german ck3Province = 17";
	const mappers::ReligionMapping theMapping(input);

	ASSERT_EQ("dutch", *theMapping.religionMatch("german", 17, 45));
}

TEST(Mappers_ReligionMappingTests, matchOnProvinceFailsForWrongProvince)
{
	std::stringstream input;
	input << "ck3 = dutch imp = german ck3Province = 17";
	const mappers::ReligionMapping theMapping(input);

	ASSERT_FALSE(theMapping.religionMatch("german", 19, 345));
}

TEST(Mappers_ReligionMappingTests, matchOnProvinceFailsForNoProvince)
{
	std::stringstream input;
	input << "ck3 = dutch imp = german ck3Province = 17";
	const mappers::ReligionMapping theMapping(input);

	ASSERT_FALSE(theMapping.religionMatch("german", 0, 345));
}

TEST(Mappers_ReligionMappingTests, matchOnRegion)
{
	auto theMapper = std::make_shared<mappers::CK3RegionMapper>();
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } } \n";
	landedTitlesStream << "k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n";
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

	ASSERT_EQ("dutch", *theMapping.religionMatch("german", 4, 34345));
}

TEST(Mappers_ReligionMappingTests, matchOnRegionFailsForWrongRegion)
{
	auto theMapper = std::make_shared<mappers::CK3RegionMapper>();
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } } \n";
	landedTitlesStream << "k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n";
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

	ASSERT_FALSE(theMapping.religionMatch("german", 79, 34345));
}

TEST(Mappers_ReligionMappingTests, matchOnRegionFailsForNoRegion)
{
	auto ck3Mapper = std::make_shared<mappers::CK3RegionMapper>();
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } } \n";
	landedTitlesStream << "k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_wakaba } }";
	regionStream << "test_region2 = { duchies = { d_hujhu } }\n";
	std::stringstream islandRegionStream;
	ck3Mapper->loadRegions(landedTitles, regionStream, islandRegionStream);

	std::stringstream input;
	input << "ck3 = dutch imp = german ck3Region = test_region3";
	mappers::ReligionMapping theMapping(input);
	theMapping.insertCK3RegionMapper(ck3Mapper);
	theMapping.insertImperatorRegionMapper(std::make_shared<mappers::ImperatorRegionMapper>());

	ASSERT_FALSE(theMapping.religionMatch("german", 17, 34345));
}

TEST(Mappers_ReligionMappingTests, matchOnRegionFailsForNoProvince)
{
	auto theMapper = std::make_shared<mappers::CK3RegionMapper>();
	CK3::LandedTitles landedTitles;
	std::stringstream landedTitlesStream;
	landedTitlesStream << "k_ugada = { d_wakaba = { c_athens = { b_athens = { province = 79 } b_newbarony = { province = 56 } } } } \n";
	landedTitlesStream << "k_ghef = { d_hujhu = { c_defff = { b_cringe = { province = 6 } b_newbarony2 = { province = 4 } } } } \n";
	landedTitles.loadTitles(landedTitlesStream);
	std::stringstream regionStream;
	regionStream << "test_region = { duchies = { d_wakaba } }";
	regionStream << "test_region2 = { duchies = { d_hujhu } }\n";
	std::stringstream islandRegionStream;
	theMapper->loadRegions(landedTitles, regionStream, islandRegionStream);

	std::stringstream input;
	input << "ck3 = dutch imp = german ck3Region = d_hujhu";
	mappers::ReligionMapping theMapping(input);
	theMapping.insertCK3RegionMapper(theMapper);
	theMapping.insertImperatorRegionMapper(std::make_shared<mappers::ImperatorRegionMapper>());

	ASSERT_FALSE(theMapping.religionMatch("german", 0, 56756));
}
