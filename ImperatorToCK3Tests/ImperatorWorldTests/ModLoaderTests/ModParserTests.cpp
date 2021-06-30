#include "Imperator/ModLoader/ModParser.h"
#include "gtest/gtest.h"



TEST(ImperatorWorld_ModTests, primitivesDefaultToBlank) {
	std::stringstream input;
	const Imperator::ModParser theMod(input);

	EXPECT_TRUE(theMod.getName().empty());
	EXPECT_TRUE(theMod.getPath().empty());
}

TEST(ImperatorWorld_ModTests, primitivesCanBeSet) {
	std::stringstream input;
	input << "name=modName\n";
	input << "path=modPath\n";

	const Imperator::ModParser theMod(input);
	EXPECT_EQ("modName", theMod.getName());
	EXPECT_EQ("modPath", theMod.getPath());
}

TEST(ImperatorWorld_ModTests, pathCanBeSetFromArchive) {
	std::stringstream input;
	input << "archive=modPath\n";

	const Imperator::ModParser theMod(input);
	EXPECT_EQ("modPath", theMod.getPath());
}

TEST(ImperatorWorld_ModTests, modIsInvalidIfPathOrNameUnSet) {
	std::stringstream input;
	Imperator::ModParser theMod(input);
	EXPECT_FALSE(theMod.isValid());

	std::stringstream input2;
	input2 << "name=modName\n";
	const Imperator::ModParser theMod2(input2);
	EXPECT_FALSE(theMod2.isValid());

	std::stringstream input3;
	input3 << "path=modPath\n";
	Imperator::ModParser theMod3(input3);
	EXPECT_FALSE(theMod3.isValid());
}

TEST(ImperatorWorld_ModTests, modIsValidIfNameAndPathSet) {
	std::stringstream input;
	input << "name=modName\n";
	input << "path=modPath\n";

	const Imperator::ModParser theMod(input);
	EXPECT_TRUE(theMod.isValid());
}

TEST(ImperatorWorld_ModTests, modIsCompressedForZipAndBinPaths) {
	std::stringstream input;
	input << "path=modPath\n";
	const Imperator::ModParser theMod(input);
	EXPECT_FALSE(theMod.isCompressed());

	std::stringstream input2;
	input2 << "path=modPath.zip\n";
	const Imperator::ModParser theMod2(input2);
	EXPECT_TRUE(theMod2.isCompressed());

	std::stringstream input3;
	input3 << "path=modPath.bin\n";
	const Imperator::ModParser theMod3(input3);
	EXPECT_TRUE(theMod3.isCompressed());
}