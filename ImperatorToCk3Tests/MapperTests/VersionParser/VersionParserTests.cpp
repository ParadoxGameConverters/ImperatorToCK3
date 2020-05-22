#include "../ImperatorToCK3/Source/Mappers/VersionParser/VersionParser.h"
#include "gtest/gtest.h"
#include <sstream>

TEST(Mappers_VersionParserTests, nameDefaultsToBlank)
{
	std::stringstream input;

	const mappers::VersionParser theParser(input);

	ASSERT_TRUE(theParser.getName().empty());
}

TEST(Mappers_VersionParserTests, nameCanBeSet)
{
	std::stringstream input;
	input << "name = test\n";

	const mappers::VersionParser theParser(input);

	ASSERT_EQ(theParser.getName(), "test");
}

TEST(Mappers_VersionParserTests, versionDefaultsToBlank)
{
	std::stringstream input;

	const mappers::VersionParser theParser(input);

	ASSERT_TRUE(theParser.getVersion().empty());
}

TEST(Mappers_VersionParserTests, versionCanBeSet)
{
	std::stringstream input;
	input << "version = test\n";

	const mappers::VersionParser theParser(input);

	ASSERT_EQ(theParser.getVersion(), "test");
}

TEST(Mappers_VersionParserTests, descriptionDefaultsToBlank)
{
	std::stringstream input;

	const mappers::VersionParser theParser(input);

	ASSERT_TRUE(theParser.getDescription().empty());
}

TEST(Mappers_VersionParserTests, descriptionCanBeSet)
{
	std::stringstream input;
	input << "descriptionLine = test\n";

	const mappers::VersionParser theParser(input);

	ASSERT_EQ(theParser.getDescription(), "test");
}
