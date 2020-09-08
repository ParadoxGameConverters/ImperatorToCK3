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

TEST(Mappers_CultureMapperTests, cultureFailsWithNoReligion)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_FALSE(culMapper.cultureMatch("test", "", 0, ""));
}

TEST(Mappers_CultureMapperTests, cultureMatchesWithCapital)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion province = 4 }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_EQ("culture", *culMapper.cultureMatch("test", "thereligion", 4, ""));
}

TEST(Mappers_CultureMapperTests, cultureFailsWithWrongCapital)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion province = 4 }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_FALSE(culMapper.cultureMatch("test", "thereligion", 3, ""));
}

TEST(Mappers_CultureMapperTests, cultureMatchesWithOwnerTitle)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion province = 4 owner = e_roman_empire }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_EQ("culture", *culMapper.cultureMatch("test", "thereligion", 0, "e_roman_empire"));
}

TEST(Mappers_CultureMapperTests, cultureFailsWithWrongOwnerTitle)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion province = 4 owner = e_roman_empire }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_FALSE(culMapper.cultureMatch("test", "", 0, "e_reman_empire"));
}


TEST(Mappers_CultureMapperTests, nonMatchGivesEmptyOptionalWithNonReligiousMatch)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = culture }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_FALSE(culMapper.cultureNonReligiousMatch("nonMatchingCulture", "", 0, ""));
}

TEST(Mappers_CultureMapperTests, simpleCultureMatchesWithNonReligiousMatch)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = test }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_EQ("culture", *culMapper.cultureNonReligiousMatch("test", "", 0, ""));
}

TEST(Mappers_CultureMapperTests, simpleCultureCorrectlyMatchesWithNonReligiousMatch)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_EQ("culture", *culMapper.cultureNonReligiousMatch("test", "", 0, ""));
}

TEST(Mappers_CultureMapperTests, cultureFailsWithCorrectReligionWithNonReligiousMatch)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_FALSE(culMapper.cultureNonReligiousMatch("test", "thereligion", 0, ""));
}

TEST(Mappers_CultureMapperTests, cultureFailsWithWrongReligionWithNonReligiousMatch)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_FALSE(culMapper.cultureNonReligiousMatch("test", "unreligion", 0, ""));
}

TEST(Mappers_CultureMapperTests, cultureFailsWithNoReligionWithNonReligiousMatch)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi religion = thereligion }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_FALSE(culMapper.cultureNonReligiousMatch("test", "", 0, ""));
}

TEST(Mappers_CultureMapperTests, cultureMatchesWithReligionAndNonReligiousLinkWithNonReligiousMatch)
{
	std::stringstream input;
	input << "link = { ck3 = culture imp = qwe imp = test imp = poi }";
	const mappers::CultureMapper culMapper(input);

	ASSERT_EQ("culture", *culMapper.cultureNonReligiousMatch("test", "thereligion", 0, ""));
}