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

TEST(ImperatorWorld_CountryNameTests, adjLocBlockReturnsCorrectLocForRevolts) {
	std::stringstream input;

	input << "adjective = CIVILWAR_FACTION_ADJECTIVE \n base = { name = someName adjective = someAdjective }";
	const auto countryName = std::move(*Imperator::CountryName::Factory().getCountryName(input));

	auto locMapper = mappers::LocalizationMapper{};
	locMapper.addLocalization("CIVILWAR_FACTION_ADJECTIVE", mappers::LocBlock{ .english = "$ADJ$" });
	locMapper.addLocalization("someAdjective", mappers::LocBlock{ .english = "Roman" });
	ASSERT_EQ("Roman", countryName.getAdjectiveLocBlock(locMapper, {})->english);
}

TEST(ImperatorWorld_CountryNameTests, nameLocBlockDefaultsToNullopt) {
	std::stringstream input;
	const auto countryName = std::move(*Imperator::CountryName::Factory().getCountryName(input));

	auto locMapper = mappers::LocalizationMapper{};
	ASSERT_EQ(std::nullopt, countryName.getNameLocBlock(locMapper, {}));
}

TEST(ImperatorWorld_CountryNameTests, nameLocBlockReturnsCorrectLocForRevolts) {
	std::stringstream input;
	input << "name = CIVILWAR_FACTION_NAME\n base = { name = someName adjective = someAdjective }";
	const auto countryName = std::move(*Imperator::CountryName::Factory().getCountryName(input));

	auto locMapper = mappers::LocalizationMapper{};
	locMapper.addLocalization("CIVILWAR_FACTION_NAME", mappers::LocBlock{ .english = "$ADJ$ Revolt" });
	locMapper.addLocalization("someAdjective", mappers::LocBlock{ .english = "Roman" });
	ASSERT_EQ("Roman Revolt", countryName.getNameLocBlock(locMapper, {})->english);
}