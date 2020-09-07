#include "../../../ImperatorToCK3/Source/Mappers/ProvinceMapper/ProvinceMapper.h"
#include "gtest/gtest.h"
#include <sstream>


TEST(Mappers_ProvinceMapperTests, emptyMappingsDefaultToEmpty)
{
	std::stringstream input;
	input << "0.0.0.0 = {\n";
	input << "}";

	const mappers::ProvinceMapper theMapper(input);

	ASSERT_TRUE(theMapper.getImperatorProvinceNumbers(1).empty());
}

TEST(Mappers_ProvinceMapperTests, canLookupImpProvinces)
{
	std::stringstream input;
	input << "0.0.0.0 = {\n";
	input << "	link = { ck3 = 2 ck3 = 1 imp = 2 imp = 1 }\n";
	input << "}";

	const mappers::ProvinceMapper theMapper(input);

	ASSERT_EQ(2, theMapper.getImperatorProvinceNumbers(1).size());
	ASSERT_EQ(2, theMapper.getImperatorProvinceNumbers(1)[0]);
	ASSERT_EQ(1, theMapper.getImperatorProvinceNumbers(1)[1]);
	ASSERT_EQ(2, theMapper.getImperatorProvinceNumbers(2).size());
	ASSERT_EQ(2, theMapper.getImperatorProvinceNumbers(2)[0]);
	ASSERT_EQ(1, theMapper.getImperatorProvinceNumbers(2)[1]);
}

TEST(Mappers_ProvinceMapperTests, canLookupCK3Provinces)
{
	std::stringstream input;
	input << "0.0.0.0 = {\n";
	input << "	link = { ck3 = 2 ck3 = 1 imp = 2 imp = 1 }\n";
	input << "}";

	const mappers::ProvinceMapper theMapper(input);

	ASSERT_EQ(2, theMapper.getCK3ProvinceNumbers(1).size());
	ASSERT_EQ(2, theMapper.getCK3ProvinceNumbers(1)[0]);
	ASSERT_EQ(1, theMapper.getCK3ProvinceNumbers(1)[1]);
	ASSERT_EQ(2, theMapper.getCK3ProvinceNumbers(2).size());
	ASSERT_EQ(2, theMapper.getCK3ProvinceNumbers(2)[0]);
	ASSERT_EQ(1, theMapper.getCK3ProvinceNumbers(2)[1]);
}
