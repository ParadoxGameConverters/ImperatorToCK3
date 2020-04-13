#include "gtest/gtest.h"
#include "../../ImperatorToCK3/Source/Common/Version.h"
#include <sstream>

TEST(ImperatorWorld_VersionTests, VersionSmaller)
{
	const Version version = Version("1.1");
	const Version requiredVersion = Version("1.3.2");
	ASSERT_TRUE(version < requiredVersion);
}

TEST(ImperatorWorld_VersionTests, VersionEqual)
{
	const Version version = Version("1.3.3.0");
	const Version requiredVersion = Version("1.3.3");
	ASSERT_EQ(version, requiredVersion);
	ASSERT_TRUE(version == requiredVersion);
}