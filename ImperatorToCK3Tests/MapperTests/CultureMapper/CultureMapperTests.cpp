#include "../../ImperatorToCK3/Source/Mappers/CultureMapper/CultureMapper.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_CultureMapperTests, nonMatchGivesEmptyOptional)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = culture }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_FALSE(culMapper.cultureMatch("nonMatchingCulture", "", 0, ""));
}

TEST(Mappers_CultureMapperTests, simpleCultureMatches)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = test }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_EQ("culture", *culMapper.cultureMatch("test", "", 0, ""));
}

TEST(Mappers_CultureMapperTests, simpleCultureCorrectlyMatches)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_EQ("culture", *culMapper.cultureMatch("test", "", 0, ""));
}

TEST(Mappers_CultureMapperTests, cultureMatchesWithReligion)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_EQ("culture", *culMapper.cultureMatch("test", "thereligion", 0, ""));
}

TEST(Mappers_CultureMapperTests, cultureFailsWithWrongReligion)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_FALSE(culMapper.cultureMatch("test", "unreligion", 0, ""));
}

TEST(Mappers_CultureMapperTests, cultureMatchesWithCapital)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion province = 4 }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_EQ("culture", *culMapper.cultureMatch("test", "", 4, ""));
}

TEST(Mappers_CultureMapperTests, cultureFailsWithWrongCapital)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion province = 4 }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_FALSE(culMapper.cultureMatch("test", "", 3, ""));
}

TEST(Mappers_CultureMapperTests, cultureMatchesWithOwnerTag)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion province = 4 owner = TAG }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_EQ("culture", *culMapper.cultureMatch("test", "", 0, "TAG"));
}

TEST(Mappers_CultureMapperTests, cultureFailsWithWrongTag)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion province = 4 owner = TAG }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_FALSE("GAT", culMapper.cultureMatch("test", "", 0));
}
