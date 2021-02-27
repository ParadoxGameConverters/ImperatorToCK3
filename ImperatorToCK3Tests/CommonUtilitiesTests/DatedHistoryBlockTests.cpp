#include "CommonUtilities/DatedHistoryBlock.h"
#include "gtest/gtest.h"
#include <sstream>


TEST(CommonUtilities_DatedHistoryBlockTests, allValidPairsAreRead)
{
	std::stringstream input;
	input << R"( = {
					culture = cuman
					culture = bashkiri
					religion = jewish
					title = "c_sarkel"
					development = 500
					monthly_alien_sightings = 5
				})";

	auto contents = DatedHistoryBlock{ input }.getContents();

	ASSERT_EQ(5, contents.size());
	ASSERT_EQ("cuman", contents.at("culture")[0]);
	ASSERT_EQ("bashkiri", contents.at("culture")[1]);
	ASSERT_EQ("jewish", contents.at("religion").back());
	ASSERT_EQ("c_sarkel", contents.at("title").back());
	ASSERT_EQ("500", contents.at("development").back());
	ASSERT_EQ("5", contents.at("monthly_alien_sightings").back());
}


TEST(CommonUtilities_DatedHistoryBlockTests, quotedStringsAreNotReadAsKeys)
{
	std::stringstream input;
	input << R"( = {
					culture = cuman
					"religion" = jewish
				})";

	const auto& contents = DatedHistoryBlock{ input }.getContents();

	ASSERT_EQ(1, contents.size());
	ASSERT_FALSE(contents.contains(R"("religion")"));
}
