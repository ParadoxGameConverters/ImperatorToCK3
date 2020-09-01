#include "../../../ImperatorToCK3/Source/Mappers/ProvinceMapper/ProvinceMapper.h"
#include "gtest/gtest.h"
#include <sstream>


TEST(Mappers_ProvinceMapperTests, emptyMappingsDefaultToEmpty)
{
	std::stringstream input;
	input << "0.0.0.0 = {\n";
	input << "}";

	const mappers::ProvinceMapper theMapper(input);

	ASSERT_TRUE(theMapper.getImpProvinceNumbers(1).empty());
}

TEST(Mappers_ProvinceMapperTests, canLookupCK2Provinces)
{
	std::stringstream input;
	input << "0.0.0.0 = {\n";
	input << "	link = { ck3 = 2 ck3 = 1 imp = 2 imp = 1 }\n";
	input << "}";

	const mappers::ProvinceMapper theMapper(input);

	ASSERT_EQ(theMapper.getImpProvinceNumbers(1).size(), 2);
	ASSERT_EQ(theMapper.getImpProvinceNumbers(1)[0], 2);
	ASSERT_EQ(theMapper.getImpProvinceNumbers(1)[1], 1);
	ASSERT_EQ(theMapper.getImpProvinceNumbers(2).size(), 2);
	ASSERT_EQ(theMapper.getImpProvinceNumbers(2)[0], 2);
	ASSERT_EQ(theMapper.getImpProvinceNumbers(2)[1], 1);
}

TEST(Mappers_ProvinceMapperTests, canLookupEU4Provinces)
{
	std::stringstream input;
	input << "0.0.0.0 = {\n";
	input << "	link = { ck3 = 2 ck3 = 1 imp = 2 imp = 1 }\n";
	input << "}";

	const mappers::ProvinceMapper theMapper(input);

	ASSERT_EQ(theMapper.getCK3ProvinceNumbers(1).size(), 2);
	ASSERT_EQ(theMapper.getCK3ProvinceNumbers(1)[0], 2);
	ASSERT_EQ(theMapper.getCK3ProvinceNumbers(1)[1], 1);
	ASSERT_EQ(theMapper.getCK3ProvinceNumbers(2).size(), 2);
	ASSERT_EQ(theMapper.getCK3ProvinceNumbers(2)[0], 2);
	ASSERT_EQ(theMapper.getCK3ProvinceNumbers(2)[1], 1);
}
