#include "gtest/gtest.h"
#include "../commonItems/CommonFunctions.h"

TEST(ImperatorWorld_CommonFunctionsTests, TrimPathTrimsSlashes)
{
	const std::string input = "/this/is/a/path.txt";

	ASSERT_EQ("path.txt", trimPath(input));
}

TEST(ImperatorWorld_CommonFunctionsTests, TrimPathTrimsBackslashes)
{
	const std::string input = R"(c:\this\is\a\path.txt)";

	ASSERT_EQ("path.txt", trimPath(input));
}

TEST(ImperatorWorld_CommonFunctionsTests, TrimPathTrimsMixedSlashes)
{
	const std::string input = "c:\\this\\is/a/path.txt";

	ASSERT_EQ("path.txt", trimPath(input));
}

TEST(ImperatorWorld_CommonFunctionsTests, TrimExtensionTrimsDot)
{
	const std::string input = "file.extension";

	ASSERT_EQ("file", trimExtension(input));
}

TEST(ImperatorWorld_CommonFunctionsTests, TrimExtensionTrimsLastDot)
{
	const std::string input = "file.name.with.extension";

	ASSERT_EQ("file.name.with", trimExtension(input));
}

TEST(ImperatorWorld_CommonFunctionsTests, ReplaceCharacterCanReplaceSpaces)
{
	const std::string input = "a file name.rome";

	ASSERT_EQ("a_file_name.rome", replaceCharacter(input, ' '));
}

TEST(ImperatorWorld_CommonFunctionsTests, ReplaceCharacterCanReplaceMinuses)
{
	const std::string input = "a file-with-name.rome";

	ASSERT_EQ("a file_with_name.rome", replaceCharacter(input, '-'));
}
