#include "gtest/gtest.h"
#include "../ImperatorToCK3/Source/CK3/Province/ProvinceDetails.h"
#include <sstream>


TEST(CK3World_CK3ProvinceDetailsTests, cultureDefaultsToEmpty)
{
	const CK3::ProvinceDetails details;

	ASSERT_EQ("", details.culture);
}

TEST(CK3World_CK3ProvinceDetailsTests, religionDefaultsToEmpty)
{
	const CK3::ProvinceDetails details;

	ASSERT_EQ("", details.religion);
}

TEST(CK3World_CK3ProvinceDetailsTests, detailsCanBeLoadedFromPath)
{
	const CK3::ProvinceDetails details("TestFiles/CK3ProvinceDetails/CK3ProvinceDetailsCorrect.txt");

	ASSERT_EQ("roman", details.culture);
	ASSERT_EQ("orthodox", details.religion);
}

TEST(CK3World_CK3ProvinceDetailsTests, detailsCanBeLoadedFromStream)
{
	std::stringstream input;
	input << "= { religion = orthodox\n random_param = random_stuff\n culture = roman\n}";
		
	const CK3::ProvinceDetails details(input);

	ASSERT_EQ("roman", details.culture);
	ASSERT_EQ("orthodox", details.religion);
}

TEST(CK3World_CK3ProvinceDetailsTests, detailsCanBeUpdatedFromPath)
{
	CK3::ProvinceDetails details;
	details.culture = "culture";
	details.religion = "religion";

	ASSERT_EQ("culture", details.culture);
	ASSERT_EQ("religion", details.religion);
	details.updateWith("TestFiles/CK3ProvinceDetails/CK3ProvinceDetailsCorrect.txt");
	ASSERT_EQ("roman", details.culture);
	ASSERT_EQ("orthodox", details.religion);
}

TEST(CK3World_CK3ProvinceDetailsTests, detailsLoadedFromBlankFileAreBlank)
{
	const CK3::ProvinceDetails details("TestFiles/CK3ProvinceDetails/CK3ProvinceDetailsBlank.txt");
	ASSERT_EQ("", details.culture);
	ASSERT_EQ("", details.religion);
}

TEST(CK3World_CK3ProvinceDetailsTests, updateWithWrongFilePathResultsInLogError)
{
	CK3::ProvinceDetails details;

	std::stringstream log;
	auto* stdOutBuf = std::cout.rdbuf();
	std::cout.rdbuf(log.rdbuf());

	details.updateWith("TestFiles/CK3ProvinceDetails/CK3ProvinceDetailsMissing.txt");

	std::cout.rdbuf(stdOutBuf);
	auto stringLog = log.str();
	const auto newLine = stringLog.find_first_of('\n');
	stringLog = stringLog.substr(0, newLine);
	

	ASSERT_EQ("   [ERROR] Could not open TestFiles/CK3ProvinceDetails/CK3ProvinceDetailsMissing.txt to update province details.", stringLog);
}

TEST(CK3World_CK3ProvinceDetailsTests, provinceDetailsWithWrongFilePathResultsInLogError)
{
	std::stringstream log;
	auto* stdOutBuf = std::cout.rdbuf();
	std::cout.rdbuf(log.rdbuf());

	const CK3::ProvinceDetails details("TestFiles/CK3ProvinceDetails/CK3ProvinceDetailsMissing.txt");

	std::cout.rdbuf(stdOutBuf);
	auto stringLog = log.str();
	const auto newLine = stringLog.find_first_of('\n');
	stringLog = stringLog.substr(0, newLine);

	ASSERT_EQ("   [ERROR] Could not open TestFiles/CK3ProvinceDetails/CK3ProvinceDetailsMissing.txt to load province details.", stringLog);
}

