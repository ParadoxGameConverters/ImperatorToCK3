#include "../../../ImperatorToCK3/Source/Mappers/ProvinceMapper/ProvinceMapping.h"
#include "gtest/gtest.h"
#include <sstream>


TEST(Mappers_ProvinceMappingTests, CK3ProvinceCanBeAdded)
{
	std::stringstream input;
	input << "= { ck3 = 2 ck3 = 1 }";

	const mappers::ProvinceMapping theMapper(input);

	ASSERT_EQ(2, theMapper.getCK3Provinces()[0]);
	ASSERT_EQ(1, theMapper.getCK3Provinces()[1]);
}

TEST(Mappers_ProvinceMappingTests, impProvinceCanBeAdded)
{
	std::stringstream input;
	input << "= { imp = 2 imp = 1 }";

	const mappers::ProvinceMapping theMapper(input);

	ASSERT_EQ(2, theMapper.getImpProvinces()[0]);
	ASSERT_EQ(1, theMapper.getImpProvinces()[1]);
}
