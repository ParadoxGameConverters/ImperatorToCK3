#include "gtest/gtest.h"
#include "CommonUtilities/DatedHistoryBlock.h"
#include <sstream>



TEST(CommonUtilities_DatedHistoryBlockTests, onlyRegisteredPairsAreRead) {
	std::stringstream input;
	input << R"( = {
					culture = cuman
					culture = bashkiri
					religion = jewish
					title = "c_sarkel"
					development = 500
					monthly_alien_sightings = 5
				})";

	auto datedHistoryBlock = DatedHistoryBlock{
		{
			{ "culture", "culture", ""},
			{ "title", "title", ""},
			{ "monthly_alien_sightings", "monthly_alien_sightings", "0" }
		},
		{},
		input
	};
	auto contents = datedHistoryBlock.getContents();

	ASSERT_EQ(3, contents.simpleFieldContents.size());

	ASSERT_EQ("cuman", contents.simpleFieldContents.at("culture")[0]);
	ASSERT_EQ("bashkiri", contents.simpleFieldContents.at("culture")[1]);
	ASSERT_EQ("c_sarkel", contents.simpleFieldContents.at("title").back());
	ASSERT_EQ("5", contents.simpleFieldContents.at("monthly_alien_sightings").back());

	ASSERT_FALSE(contents.simpleFieldContents.contains("religion"));
	ASSERT_FALSE(contents.simpleFieldContents.contains("development"));
}


TEST(CommonUtilities_DatedHistoryBlockTests, quotedStringsAreNotReadAsKeys) {
	std::stringstream input;
	input << R"( = {
					culture = cuman
					"religion" = jewish
				})";

	const auto& contents = DatedHistoryBlock{ { { "religion", "religion", std::nullopt } }, {}, input }.getContents();

	ASSERT_EQ(1, contents.simpleFieldContents.size());
	ASSERT_FALSE(contents.simpleFieldContents.contains(R"("religion")"));
}
