#include "../../ImperatorToCK3/Source/Mappers/CultureMapper/CultureMappingRule.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_CultureMappingTests, ck3CultureDefaultsToBlank)
{
	std::stringstream input;
	input << "= {}";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_TRUE(theMapping.getCK3Culture().empty());
}

TEST(Mappers_CultureMappingTests, ck3CultureCanBeSet)
{
	std::stringstream input;
	input << "= { ck3 = ck3Culture }";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_EQ(theMapping.getCK3Culture(), "ck3Culture");
}

TEST(Mappers_CultureMappingTests, impCulturesDefaultToEmpty)
{
	std::stringstream input;
	input << "= {}";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_TRUE(theMapping.getImperatorCultures().empty());
}

TEST(Mappers_CultureMappingTests, impCulturesCanBeSet)
{
	std::stringstream input;
	input << "= { imp = culture1 imp = culture2 }";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_EQ(theMapping.getImperatorCultures().size(), 2);
	ASSERT_EQ(*theMapping.getImperatorCultures().find("culture1"), "culture1");
	ASSERT_EQ(*theMapping.getImperatorCultures().find("culture2"), "culture2");
}

TEST(Mappers_CultureMappingTests, ReligionsDefaultToEmpty)
{
	std::stringstream input;
	input << "= {}";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_TRUE(theMapping.getReligions().empty());
}

TEST(Mappers_CultureMappingTests, ReligionsCanBeSet)
{
	std::stringstream input;
	input << "= { religion = religion1 religion = religion2 }";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_EQ(theMapping.getReligions().size(), 2);
	ASSERT_EQ(*theMapping.getReligions().find("religion1"), "religion1");
	ASSERT_EQ(*theMapping.getReligions().find("religion2"), "religion2");
}


TEST(Mappers_CultureMappingTests, RegionsDefaultToEmpty)
{
	std::stringstream input;
	input << "= {}";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_TRUE(theMapping.getReligions().empty());
}

TEST(Mappers_CultureMappingTests, OwnersDefaultToEmpty)
{
	std::stringstream input;
	input << "= {}";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_TRUE(theMapping.getOwners().empty());
}

TEST(Mappers_CultureMappingTests, OwnersCanBeSet)
{
	std::stringstream input;
	input << "= { owner = TAG1 owner = TAG2 }";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_EQ(theMapping.getOwners().size(), 2);
	ASSERT_EQ(*theMapping.getOwners().find("TAG1"), "TAG1");
	ASSERT_EQ(*theMapping.getOwners().find("TAG2"), "TAG2");
}

TEST(Mappers_CultureMappingTests, ProvincesDefaultToEmpty)
{
	std::stringstream input;
	input << "= {}";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_TRUE(theMapping.getProvinces().empty());
}

TEST(Mappers_CultureMappingTests, ProvincesCanBeSet)
{
	std::stringstream input;
	input << "= { province = 3 province = 4 }";

	const mappers::CultureMappingRule theMapping(input);

	ASSERT_EQ(theMapping.getProvinces().size(), 2);
	ASSERT_EQ(*theMapping.getProvinces().find(3), 3);
	ASSERT_EQ(*theMapping.getProvinces().find(4), 4);
}
