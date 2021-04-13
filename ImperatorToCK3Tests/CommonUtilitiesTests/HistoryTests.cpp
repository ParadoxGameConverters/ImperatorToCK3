#include "gtest/gtest.h"
#include "CommonUtilities/HistoryFactory.h"
#include <sstream>



TEST(CommonUtilities_HistoryTests, initialValueIsUsedAsFallback) {
	std::stringstream input;
	input << R"( = {		#Sarkel
						750.1.2 = {
							religion = kabarism
						}
						1000.1.2 = {
							culture = cuman
						}
				})";

	History::Factory provHistoryFactory(
		{
			{ "culture", "culture", std::nullopt },
			{ "religion", "religion", std::nullopt },
			{ "holding", "holding", "none" }
		},
		{}
	);

	const auto provHistory = provHistoryFactory.getHistory(input);

	ASSERT_FALSE(provHistory->getSimpleFieldValue("culture", date{1,1,1}));
	ASSERT_FALSE(provHistory->getSimpleFieldValue("religion", date{1,1,1}));
	ASSERT_EQ("none", provHistory->getSimpleFieldValue("holding", date{1,1,1}).value());
}

TEST(CommonUtilities_HistoryTests, initialValueIsOverriden) {
	std::stringstream input;
	input << R"( = {		#Sarkel
						culture = khazar
						religion = tengri_pagan
						holding = tribal_holding
						750.1.2 = {
							religion = kabarism
						}
						1000.1.2 = {
							culture = cuman
						}
				})";

	History::Factory provHistoryFactory(
		{
			{ "culture", "culture", std::nullopt },
			{ "religion", "religion", std::nullopt },
			{ "holding", "holding", "none" }
		},
		{}
	);

	const auto provHistory = provHistoryFactory.getHistory(input);

	ASSERT_EQ("khazar", provHistory->getSimpleFieldValue("culture", date{1,1,1}).value());
	ASSERT_EQ("tengri_pagan", provHistory->getSimpleFieldValue("religion", date{1,1,1}).value());
	ASSERT_EQ("tribal_holding", provHistory->getSimpleFieldValue("holding", date{1,1,1}).value());
}

TEST(CommonUtilities_HistoryTests, datedBlockCanChangeFieldValue) {
	std::stringstream input;
	input << R"( = {		#Sarkel
						culture = khazar
						religion = tengri_pagan
						holding = tribal_holding
						750.1.2 = {
							religion = kabarism
						}
						1000.1.2 = {
							culture = cuman
						}
				})";

	History::Factory provHistoryFactory(
		{
			{ "culture", "culture", std::nullopt },
			{ "religion", "religion", std::nullopt },
			{ "holding", "holding", "none" }
		},
		{}
	);

	const auto provHistory = provHistoryFactory.getHistory(input);

	ASSERT_EQ("tengri_pagan", provHistory->getSimpleFieldValue("religion", date{750,1,1}).value());
	ASSERT_EQ("kabarism", provHistory->getSimpleFieldValue("religion", date{750,1,2}).value());
	ASSERT_EQ("khazar", provHistory->getSimpleFieldValue("culture", date{1000,1,1}).value());
	ASSERT_EQ("cuman", provHistory->getSimpleFieldValue("culture", date{1000,1,3}).value());
}

TEST(CommonUtilities_HistoryTests, nulloptIsReturnedForNonExistingField) {
	std::stringstream input;
	input << R"( = {		#Sarkel
						750.1.2 = {
							religion = kabarism
						}
						1000.1.2 = {
							culture = cuman
						}
				})";

	History::Factory provHistoryFactory(
		{
			{ "culture", "culture", std::nullopt },
			{ "religion", "religion", std::nullopt },
			{ "holding", "holding", "none" }
		},
		{}
	);

	const auto provHistory = provHistoryFactory.getHistory(input);

	ASSERT_FALSE(provHistory->getSimpleFieldValue("title", date{1000,1,1}));
}