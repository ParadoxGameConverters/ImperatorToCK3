#include "gtest/gtest.h"
#include "CK3/Province/ProvinceDetails.h"
#include <sstream>



TEST(CK3World_CK3ProvinceDetailsTests, cultureDefaultsToEmpty) {
	const CK3::ProvinceDetails details;

	ASSERT_EQ("", details.culture);
}

TEST(CK3World_CK3ProvinceDetailsTests, religionDefaultsToEmpty) {
	const CK3::ProvinceDetails details;

	ASSERT_EQ("", details.religion);
}

TEST(CK3World_CK3ProvinceDetailsTests, holdingDefaultsToNone) {
	const CK3::ProvinceDetails details;

	ASSERT_EQ("none", details.holding);
}

TEST(CK3World_CK3ProvinceDetailsTests, detailsCanBeLoadedFromStream) {
	std::stringstream input;
	input << "= { religion = orthodox\n random_param = random_stuff\n culture = roman\n}";
		
	const CK3::ProvinceDetails details(input);

	ASSERT_EQ("roman", details.culture);
	ASSERT_EQ("orthodox", details.religion);
}

TEST(CK3World_CK3ProvinceDetailsTests, detailsAreLoadedFromDatedBlocks) {
	std::stringstream input;
	input << "= {"
	"religion = catholic\n"
	"random_param = random_stuff\n"
	"culture = roman\n"
	"850.1.1 = { religion=orthodox holding=castle_holding }"
	"}";

	const CK3::ProvinceDetails details(input);

	ASSERT_EQ("castle_holding", details.holding);
	ASSERT_EQ("orthodox", details.religion);
}
