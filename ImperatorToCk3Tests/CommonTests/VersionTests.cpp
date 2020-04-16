#include "gtest/gtest.h"
#include "../../ImperatorToCK3/Source/Common/Version.h"
#include <sstream>

TEST(ImperatorWorld_VersionTests, VersionHigherOrEqual)
{
	const Version version1("1.4.1");
	const Version version2("1.3.2");
	const Version requiredVersion("1.3.2.0");
	ASSERT_TRUE(version1 >= requiredVersion);
	ASSERT_TRUE(version2 >= requiredVersion);
}
TEST(ImperatorWorld_VersionTests, VersionHigher)
{
	const Version version("1.4.1");
	const Version requiredVersion("1.3.2");
	ASSERT_TRUE(version > requiredVersion);
}

TEST(ImperatorWorld_VersionTests, VersionLowerOrEqual)
{
	const Version version1("1.2.1");
	const Version version2("1.3.2");
	const Version requiredVersion("1.3.2.0");
	ASSERT_TRUE(version1 <= requiredVersion);
	ASSERT_TRUE(version2 <= requiredVersion);
}
TEST(ImperatorWorld_VersionTests, VersionLower)
{
	const Version version("1.1");
	const Version requiredVersion("1.3.2");
	ASSERT_TRUE(version < requiredVersion);
}
TEST(ImperatorWorld_VersionTests, VersionEqual)
{
	const Version version("1.3.3.0");
	const Version requiredVersion("1.3.3");
	ASSERT_EQ(version, requiredVersion);
	ASSERT_TRUE(version == requiredVersion);
}

TEST(ImperatorWorld_VersionTests, VersionNotEqual)
{
	const Version version("1.3.3.3"); 
	const Version requiredVersion("1.3.3");
	ASSERT_TRUE(version != requiredVersion);
}