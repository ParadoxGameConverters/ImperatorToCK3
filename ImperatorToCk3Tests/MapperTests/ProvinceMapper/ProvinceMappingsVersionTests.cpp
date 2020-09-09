#include "../../../ImperatorToCK3/Source/Mappers/ProvinceMapper/ProvinceMappingsVersion.h"
#include "gtest/gtest.h"
#include <sstream>
#include <string>


TEST(Mappers_ProvinceMappingsVersionTests, MappingsDefaultToEmpty)
{
	std::stringstream input;
	input << "= {\n";
	input << "}";

	const mappers::ProvinceMappingsVersion theMappingVersion(input);

	ASSERT_TRUE(theMappingVersion.getMappings().empty());
}

TEST(Mappers_ProvinceMappingsVersionTests, MappingsCanBeLoaded)
{
	std::stringstream input;
	input << "= {\n";
	input << "	link = { ck3 = 1 imp = 1 }\n";
	input << "	link = { ck3 = 2 imp = 2 }\n";
	input << "}";

	const mappers::ProvinceMappingsVersion theMappingVersion(input);

	ASSERT_EQ(2, theMappingVersion.getMappings().size());
}