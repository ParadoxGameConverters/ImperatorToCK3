#include "gtest/gtest.h"
#include "Imperator/Countries/CountryNameFactory.h"
#include "Imperator/Countries/Country.h"
#include "Mappers/LocalizationMapper/LocalizationMapper.h"
#include "Log.h"
#include <sstream>



TEST(ImperatorWorld_CountryNameTests, nameDefaultsToEmpty) {
	std::stringstream input;
	const auto countryName = std::move(*Imperator::CountryName::Factory().getCountryName(input));

	ASSERT_TRUE(countryName.getName().empty());
}

TEST(ImperatorWorld_CountryNameTests, nameCanBeSet) {
	std::stringstream input;
	input << "name = someName adjective = someAdjective";
	const auto countryName = std::move(*Imperator::CountryName::Factory().getCountryName(input));

	ASSERT_EQ("someName", countryName.getName());
}

TEST(ImperatorWorld_CountryNameTests, adjectiveDefaultsTo_ADJ) {
	std::stringstream input;
	const auto countryName = std::move(*Imperator::CountryName::Factory().getCountryName(input));

	ASSERT_EQ("_ADJ", countryName.getAdjective());
}

TEST(ImperatorWorld_CountryNameTests, adjectiveCanBeSet) {
	std::stringstream input;
	input << "name = someName adjective = someAdjective";
	const auto countryName = std::move(*Imperator::CountryName::Factory().getCountryName(input));

	ASSERT_EQ("someAdjective", countryName.getAdjective());
}

TEST(ImperatorWorld_CountryNameTests, baseDefaultsToNullptr) {
	std::stringstream input;
	const auto countryName = std::move(*Imperator::CountryName::Factory().getCountryName(input));

	ASSERT_EQ(nullptr, countryName.getBase());
}

TEST(ImperatorWorld_CountryNameTests, baseCanBeSet) {
	std::stringstream input;
	input << "name = revolt\n base = { name = someName adjective = someAdjective }";
	const auto countryName = std::move(*Imperator::CountryName::Factory().getCountryName(input));

	ASSERT_EQ("someName", countryName.getBase()->getName());
	ASSERT_EQ("someAdjective", countryName.getBase()->getAdjective());
	ASSERT_EQ(nullptr, countryName.getBase()->getBase());
}

TEST(ImperatorWorld_CountryNameTests, adjLocBlockDefaultsToNullopt) {
	std::stringstream input;
	const auto countryName = std::move(*Imperator::CountryName::Factory().getCountryName(input));

	auto locMapper = mappers::LocalizationMapper{};
	ASSERT_EQ(std::nullopt, countryName.getAdjectiveLocBlock(locMapper, {}));
}

TEST(ImperatorWorld_CountryNameTests, nameLocBlockDefaultsToNullopt) {
	std::stringstream input;
	const auto countryName = std::move(*Imperator::CountryName::Factory().getCountryName(input));

	auto locMapper = mappers::LocalizationMapper{};
	ASSERT_EQ(std::nullopt, countryName.getNameLocBlock(locMapper, {}));
}