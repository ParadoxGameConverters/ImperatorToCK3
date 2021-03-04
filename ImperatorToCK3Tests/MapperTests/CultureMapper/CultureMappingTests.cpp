#include "Mappers/CultureMapper/CultureMappingRule.h"
#include "Mappers/RegionMapper/CK3RegionMapper.h"
#include "Mappers/RegionMapper/ImperatorRegionMapper.h"
#include "CK3/Titles/LandedTitles.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_CultureMappingTests, ck3CultureDefaultsToBlank)
{
	std::stringstream input;

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_TRUE(theMapping.getCK3Culture().empty());
}

TEST(Mappers_CultureMappingTests, ck3CultureCanBeSet)
{
	std::stringstream input;
	input << "ck3 = ck3Culture";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_EQ("ck3Culture", theMapping.getCK3Culture());
}

TEST(Mappers_CultureMappingTests, impCulturesDefaultToEmpty)
{
	std::stringstream input;

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_TRUE(theMapping.getImperatorCultures().empty());
}

TEST(Mappers_CultureMappingTests, impCulturesCanBeSet)
{
	std::stringstream input;
	input << "imp = culture1 imp = culture2";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_EQ(2, theMapping.getImperatorCultures().size());
	ASSERT_EQ("culture1", *theMapping.getImperatorCultures().find("culture1"));
	ASSERT_EQ("culture2" , *theMapping.getImperatorCultures().find("culture2"));
}

TEST(Mappers_CultureMappingTests, ReligionsDefaultToEmpty)
{
	std::stringstream input;

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_TRUE(theMapping.getReligions().empty());
}

TEST(Mappers_CultureMappingTests, ReligionsCanBeSet)
{
	std::stringstream input;
	input << "religion = religion1 religion = religion2";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_EQ(theMapping.getReligions().size(), 2);
	ASSERT_EQ("religion1", *theMapping.getReligions().find("religion1"));
	ASSERT_EQ("religion2", *theMapping.getReligions().find("religion2"));
}


TEST(Mappers_CultureMappingTests, RegionsDefaultToEmpty)
{
	std::stringstream input;

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_TRUE(theMapping.getReligions().empty());
}

TEST(Mappers_CultureMappingTests, OwnersDefaultToEmpty)
{
	std::stringstream input;

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_TRUE(theMapping.getOwners().empty());
}

TEST(Mappers_CultureMappingTests, OwnersCanBeSet)
{
	std::stringstream input;
	input << "owner = TAG1 owner = TAG2";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_EQ(2, theMapping.getOwners().size());
	ASSERT_EQ("TAG1", *theMapping.getOwners().find("TAG1"));
	ASSERT_EQ("TAG2", *theMapping.getOwners().find("TAG2"));
}

TEST(Mappers_CultureMappingTests, ProvincesDefaultToEmpty)
{
	std::stringstream input;

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_TRUE(theMapping.getProvinces().empty());
}

TEST(Mappers_CultureMappingTests, ProvincesCanBeSet)
{
	std::stringstream input;
	input << "ck3Province = 3 ck3Province = 4";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_EQ(2, theMapping.getProvinces().size());
	ASSERT_EQ(3, *theMapping.getProvinces().find(3));
	ASSERT_EQ(4, *theMapping.getProvinces().find(4));
}


TEST(Mappers_CultureMappingTests, matchOnRegion)
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
	mappers::CultureMappingRule theMapping(input);
	theMapping.insertCK3RegionMapper(theMapper);
	theMapping.insertImperatorRegionMapper(std::make_shared<mappers::ImperatorRegionMapper>());

	ASSERT_EQ("dutch", *theMapping.match("german", "", 4, 0, ""));
}

TEST(Mappers_CultureMappingTests, matchOnRegionFailsForWrongRegion)
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
	mappers::CultureMappingRule theMapping(input);
	theMapping.insertCK3RegionMapper(theMapper);
	theMapping.insertImperatorRegionMapper(std::make_shared<mappers::ImperatorRegionMapper>());

	ASSERT_FALSE(theMapping.match("german", "", 79, 0, ""));
}

TEST(Mappers_CultureMappingTests, matchOnRegionFailsForNoRegion)
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
	mappers::CultureMappingRule theMapping(input);
	theMapping.insertCK3RegionMapper(ck3Mapper);
	theMapping.insertImperatorRegionMapper(std::make_shared<mappers::ImperatorRegionMapper>());

	ASSERT_FALSE(theMapping.match("german", "", 17, 0, ""));
}

TEST(Mappers_CultureMappingTests, matchOnRegionFailsForNoProvince)
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
	mappers::CultureMappingRule theMapping(input);
	theMapping.insertCK3RegionMapper(theMapper);
	theMapping.insertImperatorRegionMapper(std::make_shared<mappers::ImperatorRegionMapper>());

	ASSERT_FALSE(theMapping.match("german", "", 0, 0, ""));
}
