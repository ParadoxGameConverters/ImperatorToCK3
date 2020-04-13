#include "gtest/gtest.h"
#include "../../ImperatorToCK3/Source/Common/CommonFunctions.h"

TEST(ImperatorWorld_CommonFunctionsTests, TrimPathTrimsSlashes)
{
	const std::string input = "/this/is/a/path.txt";

	ASSERT_EQ(trimPath(input), "path.txt");
}

TEST(ImperatorWorld_CommonFunctionsTests, TrimPathTrimsBackslashes)
{
	const std::string input = "c:\\this\\is\\a\\path.txt";

	ASSERT_EQ(trimPath(input), "path.txt");
}

TEST(ImperatorWorld_CommonFunctionsTests, TrimPathTrimsMixedSlashes)
{
	const std::string input = "c:\\this\\is/a/path.txt";

	ASSERT_EQ(trimPath(input), "path.txt");
}

TEST(ImperatorWorld_CommonFunctionsTests, TrimExtensionTrimsDot)
{
	const std::string input = "file.extension";

	ASSERT_EQ(trimExtension(input), "file");
}

TEST(ImperatorWorld_CommonFunctionsTests, TrimExtensionTrimsLastDot)
{
	const std::string input = "file.name.with.extension";

	ASSERT_EQ(trimExtension(input), "file.name.with");
}

TEST(ImperatorWorld_CommonFunctionsTests, ReplaceCharacterCanReplaceSpaces)
{
	const std::string input = "a file name.rome";

	ASSERT_EQ(replaceCharacter(input, ' '), "a_file_name.rome");
}

TEST(ImperatorWorld_CommonFunctionsTests, ReplaceCharacterCanReplaceMinuses)
{
	const std::string input = "a file-with-name.rome";

	ASSERT_EQ(replaceCharacter(input, '-'), "a file_with_name.rome");
}
